using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class ReservationsController(IReservationService reservationService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReservationResponse>> Create([FromBody] ReservationRequest request, CancellationToken cancellationToken)
    {
        var response = await reservationService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await reservationService.GetByIdAsync(id, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ReservationResponse>>> List(CancellationToken cancellationToken)
    {
        return await reservationService.ListAsync(cancellationToken);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReservationResponse>> Update(int id, [FromBody] ReservationRequest request, CancellationToken cancellationToken)
    {
        var response = await reservationService.UpdateAsync(id, request, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpPatch("{id:int}/cancel")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReservationResponse>> Cancel(int id, CancellationToken cancellationToken)
    {
        var (reservation, error) = await reservationService.CancelAsync(id, cancellationToken);
        if (reservation is null && error is null) return NotFound();
        if (error is not null)
            return Conflict(new ProblemDetails { Title = "Invalid Operation", Detail = error, Status = 409 });
        return reservation!;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await reservationService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
