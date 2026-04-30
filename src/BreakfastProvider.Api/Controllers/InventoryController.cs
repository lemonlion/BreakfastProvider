using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class InventoryController(IInventoryService inventoryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<InventoryItemResponse>> Create([FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var response = await inventoryService.GetByIdAsync(id, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<InventoryItemResponse>>> List(CancellationToken cancellationToken)
    {
        return await inventoryService.ListAsync(cancellationToken);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InventoryItemResponse>> Update(int id, [FromBody] InventoryItemRequest request, CancellationToken cancellationToken)
    {
        var response = await inventoryService.UpdateAsync(id, request, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await inventoryService.DeleteAsync(id, cancellationToken);
        if (!deleted) return NotFound();
        return NoContent();
    }
}
