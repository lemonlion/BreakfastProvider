using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("order-timings")]
[Produces("application/json")]
[Consumes("application/json")]
public class OrderTimingsController(IOrderTimingService orderTimingService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<OrderTimingResponse>> Record([FromBody] OrderTimingRequest request, CancellationToken cancellationToken)
    {
        var response = await orderTimingService.RecordAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderTimingSummaryResponse>>> GetSummary(CancellationToken cancellationToken)
    {
        var results = await orderTimingService.GetSummaryAsync(cancellationToken);
        return results;
    }

    [HttpGet("station/{station}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<OrderTimingResponse>>> ListByStation(string station, CancellationToken cancellationToken)
    {
        var results = await orderTimingService.ListByStationAsync(station, cancellationToken);
        return results;
    }
}
