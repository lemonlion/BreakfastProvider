using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("chef-notes")]
[Produces("application/json")]
[Consumes("application/json")]
public class ChefNotesController(IChefNoteService chefNoteService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChefNoteResponse>> Create([FromBody] ChefNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await chefNoteService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{noteId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChefNoteResponse>> GetById(string noteId, CancellationToken cancellationToken)
    {
        var response = await chefNoteService.GetByIdAsync(noteId, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpPatch("{noteId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChefNoteResponse>> Update(string noteId, [FromBody] UpdateChefNoteRequest request, CancellationToken cancellationToken)
    {
        var response = await chefNoteService.UpdateAsync(noteId, request, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet("recipe/{recipeName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChefNoteResponse>>> ListByRecipe(string recipeName, CancellationToken cancellationToken)
    {
        var results = await chefNoteService.ListByRecipeAsync(recipeName, cancellationToken);
        return results;
    }
}
