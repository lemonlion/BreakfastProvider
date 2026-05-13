using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("recipe-costs")]
[Produces("application/json")]
[Consumes("application/json")]
public class RecipeCostsController(KafkaEventPublisher<RecipeCostCalculatedEvent> publisher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> CalculateCost([FromBody] RecipeCostRequest request, CancellationToken cancellationToken)
    {
        var costEvent = new RecipeCostCalculatedEvent
        {
            CalculationId = Guid.NewGuid(),
            RecipeName = request.RecipeName!,
            Ingredients = request.Ingredients ?? [],
            TotalCost = request.TotalCost,
            Currency = request.Currency ?? "GBP",
            CalculatedAt = DateTime.UtcNow
        };

        await publisher.PublishEvent(costEvent, cancellationToken);

        return Accepted(new { costEvent.CalculationId });
    }
}

public class RecipeCostRequest
{
    public string? RecipeName { get; set; }
    public List<string>? Ingredients { get; set; }
    public decimal TotalCost { get; set; }
    public string? Currency { get; set; }
}
