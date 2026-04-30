using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class StaffController(IStaffService staffService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StaffMemberResponse>> Create([FromBody] StaffMemberRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffMemberResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await staffService.GetByIdAsync(id, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<StaffMemberResponse>>> List(CancellationToken cancellationToken)
    {
        return await staffService.ListAsync(cancellationToken);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StaffMemberResponse>> Update(int id, [FromBody] StaffMemberRequest request, CancellationToken cancellationToken)
    {
        var response = await staffService.UpdateAsync(id, request, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await staffService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
