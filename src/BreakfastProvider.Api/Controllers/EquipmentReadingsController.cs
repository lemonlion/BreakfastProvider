using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("equipment-readings")]
[Produces("application/json")]
[Consumes("application/json")]
public class EquipmentReadingsController(IEquipmentReadingService equipmentReadingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<EquipmentReadingResponse>> Record([FromBody] EquipmentReadingRequest request, CancellationToken cancellationToken)
    {
        var response = await equipmentReadingService.RecordAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("equipment/{equipmentId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<EquipmentReadingResponse>>> ListByEquipment(string equipmentId, CancellationToken cancellationToken)
    {
        var results = await equipmentReadingService.ListByEquipmentAsync(equipmentId, cancellationToken);
        return results;
    }

    [HttpDelete("{readingId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(string readingId, CancellationToken cancellationToken)
    {
        await equipmentReadingService.DeleteAsync(readingId, cancellationToken);
        return NoContent();
    }
}
