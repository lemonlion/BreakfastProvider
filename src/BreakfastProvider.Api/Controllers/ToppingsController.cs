using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class ToppingsController(IToppingService toppingService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<ToppingResponse>> GetToppings()
    {
        return toppingService.GetAvailableToppings();
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ToppingResponse>> AddTopping([FromBody] ToppingRequest request, CancellationToken cancellationToken)
    {
        var topping = await toppingService.CreateToppingAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, topping);
    }

    [HttpPut("{toppingId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<ToppingResponse> UpdateTopping(Guid toppingId, [FromBody] UpdateToppingRequest request)
    {
        var result = toppingService.UpdateTopping(toppingId, request);
        if (result is null) return NotFound();
        return result;
    }

    [HttpDelete("{toppingId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public IActionResult DeleteTopping(Guid toppingId)
    {
        if (!toppingService.DeleteTopping(toppingId)) return NotFound();
        return NoContent();
    }
}
