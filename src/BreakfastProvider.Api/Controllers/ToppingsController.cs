using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class ToppingsController(
    IOptions<FeatureSwitchesConfig> featureSwitches,
    PubSubEventPublisher<ToppingCreatedEvent> toppingCreatedPublisher) : ControllerBase
{
    private static readonly List<ToppingResponse> Toppings =
    [
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000001"), Name = "Raspberries", Category = "Fruit" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000002"), Name = "Blueberries", Category = "Fruit" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000003"), Name = "Maple Syrup", Category = "Syrup" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000004"), Name = "Whipped Cream", Category = "Cream" },
        new() { ToppingId = Guid.Parse("11111111-0000-0000-0000-000000000005"), Name = "Chocolate Chips", Category = "Chocolate" }
    ];

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<ToppingResponse>> GetToppings()
    {
        var result = Toppings.AsEnumerable();

        if (!featureSwitches.Value.IsRaspberryToppingEnabled)
            result = result.Where(t => t.Name != "Raspberries");

        return result.ToList();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ToppingResponse>> AddTopping([FromBody] ToppingRequest request, CancellationToken cancellationToken)
    {
        var topping = new ToppingResponse
        {
            ToppingId = Guid.NewGuid(),
            Name = request.Name!,
            Category = request.Category!
        };

        await toppingCreatedPublisher.PublishEvent(new ToppingCreatedEvent
        {
            ToppingId = topping.ToppingId,
            Name = topping.Name,
            Category = topping.Category,
            IsSeasonal = false,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, topping);
    }

    [HttpPut("{toppingId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ToppingResponse> UpdateTopping(Guid toppingId, [FromBody] UpdateToppingRequest request)
    {
        var topping = Toppings.FirstOrDefault(t => t.ToppingId == toppingId);
        if (topping == null) return NotFound();

        return new ToppingResponse
        {
            ToppingId = toppingId,
            Name = request.Name!,
            Category = request.Category!
        };
    }

    [HttpDelete("{toppingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteTopping(Guid toppingId)
    {
        var topping = Toppings.FirstOrDefault(t => t.ToppingId == toppingId);
        if (topping == null) return NotFound();
        return NoContent();
    }
}
