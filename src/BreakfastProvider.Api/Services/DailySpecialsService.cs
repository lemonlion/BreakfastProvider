using System.Collections.Concurrent;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Api.Telemetry;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Services;

public class DailySpecialsService(
    IOptions<DailySpecialsConfig> config,
    IIdempotencyStore idempotencyStore,
    PubSubEventPublisher<DailySpecialOrderedEvent> dailySpecialOrderedPublisher,
    ILogger<DailySpecialsService> logger) : IDailySpecialsService
{
    private static readonly List<(Guid Id, string Name, string Description)> Specials =
    [
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000001"), "Cinnamon Swirl Pancakes", "Fluffy pancakes with cinnamon sugar swirl and cream cheese drizzle"),
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000002"), "Matcha Waffles", "Crispy green tea waffles with white chocolate chips"),
        (Guid.Parse("aaaa0000-0000-0000-0000-000000000003"), "Lemon Ricotta Pancakes", "Light and airy pancakes with fresh ricotta and lemon zest")
    ];

    private static readonly ConcurrentDictionary<Guid, int> OrderCounts = new();

    public List<DailySpecialResponse> GetAvailableSpecials()
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.GetAvailableSpecials");

        var maxOrders = config.Value.MaxOrdersPerSpecial;
        var result = Specials.Select(s => new DailySpecialResponse
        {
            SpecialId = s.Id,
            Name = s.Name,
            Description = s.Description,
            RemainingQuantity = Math.Max(0, maxOrders - OrderCounts.GetValueOrDefault(s.Id, 0))
        }).ToList();

        activity?.SetTag("daily_specials.count", result.Count);
        return result;
    }

    public async Task<DailySpecialOrderResponse?> CheckIdempotencyAsync(string? idempotencyKey, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.CheckIdempotency");
        activity?.SetTag("idempotency.key_present", idempotencyKey is not null);

        if (idempotencyKey is null)
            return null;

        var (found, statusCode, cachedResponse) =
            await idempotencyStore.TryGetAsync<DailySpecialOrderResponse>(idempotencyKey, cancellationToken);

        activity?.SetTag("idempotency.cache_hit", found);

        if (found)
        {
            logger.LogInformation("Idempotency cache hit for key {IdempotencyKey}", idempotencyKey);
            return cachedResponse;
        }

        return null;
    }

    public (Guid Id, string Name, string Description)? ValidateSpecialExists(Guid? specialId)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.ValidateSpecial");
        activity?.SetTag("daily_specials.special_id", specialId?.ToString());

        var special = Specials.FirstOrDefault(s => s.Id == specialId);
        var found = special != default;

        activity?.SetTag("daily_specials.found", found);

        return found ? special : null;
    }

    public DailySpecialOrderResponse? ReserveQuantity(Guid specialId, int quantity, string specialName)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.ReserveQuantity");
        activity?.SetTag("daily_specials.special_id", specialId.ToString());
        activity?.SetTag("daily_specials.requested_quantity", quantity);

        var maxOrders = config.Value.MaxOrdersPerSpecial;

        while (true)
        {
            var currentCount = OrderCounts.GetOrAdd(specialId, 0);

            if (currentCount + quantity > maxOrders)
            {
                activity?.SetTag("daily_specials.sold_out", true);
                logger.LogInformation("Daily special '{SpecialName}' sold out — current: {Current}, requested: {Requested}, max: {Max}",
                    specialName, currentCount, quantity, maxOrders);
                return null;
            }

            var newCount = currentCount + quantity;

            if (OrderCounts.TryUpdate(specialId, newCount, currentCount))
            {
                var remaining = Math.Max(0, maxOrders - newCount);
                activity?.SetTag("daily_specials.remaining", remaining);

                return new DailySpecialOrderResponse
                {
                    OrderConfirmationId = Guid.NewGuid(),
                    SpecialId = specialId,
                    QuantityOrdered = quantity,
                    RemainingQuantity = remaining
                };
            }
        }
    }

    public async Task StoreIdempotencyResultAsync(string? idempotencyKey, DailySpecialOrderResponse response, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.StoreIdempotencyResult");

        if (idempotencyKey is null)
        {
            activity?.SetTag("idempotency.skipped", true);
            return;
        }

        activity?.SetTag("idempotency.key", idempotencyKey);
        await idempotencyStore.SetAsync(idempotencyKey, 201, response, config.Value.IdempotencyTtlSeconds, cancellationToken);
        logger.LogInformation("Stored idempotency result for key {IdempotencyKey}", idempotencyKey);
    }

    public async Task PublishOrderEventAsync(DailySpecialOrderResponse response, string specialName, CancellationToken cancellationToken)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.PublishOrderEvent");
        activity?.SetTag("daily_specials.order_id", response.OrderConfirmationId.ToString());
        activity?.SetTag("daily_specials.special_name", specialName);

        await dailySpecialOrderedPublisher.PublishEvent(new DailySpecialOrderedEvent
        {
            OrderId = response.OrderConfirmationId,
            SpecialName = specialName,
            CustomerName = "Guest",
            RemainingOrders = response.RemainingQuantity,
            OrderedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public void ResetOrderCounts(Guid? specialId)
    {
        using var activity = DiagnosticsConfig.ActivitySource.StartActivity("DailySpecialsService.ResetOrderCounts");
        activity?.SetTag("daily_specials.reset_all", !specialId.HasValue);

        if (specialId.HasValue)
            OrderCounts.TryRemove(specialId.Value, out _);
        else
            OrderCounts.Clear();
    }
}
