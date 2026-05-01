using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("daily-specials")]
[Produces("application/json")]
[Consumes("application/json")]
public class DailySpecialsController(IDailySpecialsService dailySpecialsService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<List<DailySpecialResponse>> GetDailySpecials()
    {
        return dailySpecialsService.GetAvailableSpecials();
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

        var cached = await dailySpecialsService.CheckIdempotencyAsync(idempotencyKey, cancellationToken);
        if (cached is not null)
            return StatusCode(StatusCodes.Status201Created, cached);

        var special = dailySpecialsService.ValidateSpecialExists(request.SpecialId);
        if (special is null)
            return NotFound(new ProblemDetails
            {
                Title = "Daily special not found",
                Status = StatusCodes.Status404NotFound,
                Detail = $"No daily special found with ID '{request.SpecialId}'."
            });

        var response = dailySpecialsService.ReserveQuantity(request.SpecialId!.Value, request.Quantity, special.Value.Name);
        if (response is null)
            return Conflict(new ProblemDetails
            {
                Title = "Daily special sold out",
                Status = StatusCodes.Status409Conflict,
                Detail = $"'{special.Value.Name}' has reached the maximum orders for today."
            });

        await dailySpecialsService.StoreIdempotencyResultAsync(idempotencyKey, response, cancellationToken);
        await dailySpecialsService.PublishOrderEventAsync(response, special.Value.Name, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpDelete("orders")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public IActionResult ResetOrderCounts([FromQuery] Guid? specialId = null)
    {
        dailySpecialsService.ResetOrderCounts(specialId);
        return NoContent();
    }
}
