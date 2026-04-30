using System.Collections.Concurrent;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("daily-specials")]
[Produces("application/json")]
[Consumes("application/json")]
public class DailySpecialsController(
    IOptions<DailySpecialsConfig> config,
    IIdempotencyStore idempotencyStore,
    PubSubEventPublisher<DailySpecialOrderedEvent> dailySpecialOrderedPublisher) : ControllerBase
{
    private static readonly List<(Guid Id, string Name, string Description)> Specials =
    [
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000001"), "Cinnamon Swirl Pancakes", "Fluffy pancakes with cinnamon sugar swirl and cream cheese drizzle"),
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000002"), "Matcha Waffles", "Crispy green tea waffles with white chocolate chips"),
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000003"), "Lemon Ricotta Pancakes", "Light and airy pancakes with fresh ricotta and lemon zest")
    ];

    private static readonly ConcurrentDictionary<Guid, int> OrderCounts = new();

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<DailySpecialResponse>> GetDailySpecials()
    {
        var maxOrders = config.Value.MaxOrdersPerSpecial;
        var result = Specials.Select(s => new DailySpecialResponse
        {
            SpecialId = s.Id,
            Name = s.Name,
            Description = s.Description,
            RemainingQuantity = Math.Max(0, maxOrders - OrderCounts.GetValueOrDefault(s.Id, 0))
        }).ToList();

        return result;
    }

    [HttpPost("orders")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DailySpecialOrderResponse>> OrderDailySpecial(
        [FromBody] DailySpecialOrderRequest request,
        CancellationToken cancellationToken)
    {
        var idempotencyKey = Request.Headers["Idempotency-Key"].FirstOrDefault();

        if (idempotencyKey is not null)
        {
            var (found, statusCode, cachedResponse) =
                await idempotencyStore.TryGetAsync<DailySpecialOrderResponse>(idempotencyKey, cancellationToken);

            if (found)
                return StatusCode(statusCode, cachedResponse);
        }

        var special = Specials.FirstOrDefault(s => s.Id == request.SpecialId);
        if (special == default)
            return NotFound(new ProblemDetails
            {
                Title = "Daily special not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No daily special found with ID '{request.SpecialId}'."
            });

        var maxOrders = config.Value.MaxOrdersPerSpecial;

        // Atomically check-and-increment using a CAS (compare-and-swap) loop
        // to prevent overselling under concurrent requests.
        while (true)
        {
            var currentCount = OrderCounts.GetOrAdd(request.SpecialId!.Value, 0);

            if (currentCount + request.Quantity > maxOrders)
            {
                return Conflict(new ProblemDetails
                {
                    Title = "Daily special sold out",
                    Status = StatusCodes.Status409Conflict,
                    Detail = $"'{special.Name}' has reached the maximum of {maxOrders} orders for today."
                });
            }

            var newCount = currentCount + request.Quantity;

            // Atomically swap only if the value hasn't changed since we read it.
            // If another thread modified the count, retry the loop.
            if (OrderCounts.TryUpdate(request.SpecialId!.Value, newCount, currentCount))
            {
                var remaining = Math.Max(0, maxOrders - newCount);

                var response = new DailySpecialOrderResponse
                {
                    OrderConfirmationId = Guid.NewGuid(),
                    SpecialId = request.SpecialId!.Value,
                    QuantityOrdered = request.Quantity,
                    RemainingQuantity = remaining
                };

                if (idempotencyKey is not null)
                {
                    await idempotencyStore.SetAsync(
                        idempotencyKey,
                        StatusCodes.Status201Created,
                        response,
                        config.Value.IdempotencyTtlSeconds,
                        cancellationToken);
                }

                await dailySpecialOrderedPublisher.PublishEvent(new DailySpecialOrderedEvent
                {
                    OrderId = response.OrderConfirmationId,
                    SpecialName = special.Name,
                    CustomerName = "Guest",
                    RemainingOrders = remaining,
                    OrderedAt = DateTime.UtcNow
                }, cancellationToken);

                return StatusCode(StatusCodes.Status201Created, response);
            }

            // CAS failed — another thread modified the count. Retry.
        }
    }

    [HttpDelete("orders")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetOrderCounts([FromQuery] Guid? specialId = null)
    {
        if (specialId.HasValue)
            OrderCounts.TryRemove(specialId.Value, out _);
        else
            OrderCounts.Clear();

        return NoContent();
    }
}
