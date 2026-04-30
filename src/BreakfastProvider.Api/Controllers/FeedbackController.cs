using BreakfastProvider.Api.Models.Requests;
using BreakfastProvider.Api.Models.Responses;
using BreakfastProvider.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Produces("application/json")]
[Consumes("application/json")]
public class FeedbackController(IFeedbackService feedbackService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FeedbackResponse>> Create([FromBody] FeedbackRequest request, CancellationToken cancellationToken)
    {
        var response = await feedbackService.CreateAsync(request, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{feedbackId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FeedbackResponse>> GetById(string feedbackId, CancellationToken cancellationToken)
    {
        var response = await feedbackService.GetByIdAsync(feedbackId, cancellationToken);
        if (response is null) return NotFound();
        return response;
    }

    [HttpGet("order/{orderId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<List<FeedbackResponse>>> ListByOrder(string orderId, CancellationToken cancellationToken)
    {
        var results = await feedbackService.ListByOrderAsync(orderId, cancellationToken);
        return results;
    }
}
