using BreakfastProvider.Api.Events;
using BreakfastProvider.Api.Models.Events;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Controllers;

[ApiController]
[Route("customer-feedback")]
[Produces("application/json")]
[Consumes("application/json")]
public class CustomerFeedbackController(PubSubEventPublisher<CustomerFeedbackReceivedEvent> publisher) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> SubmitFeedback([FromBody] CustomerFeedbackRequest request, CancellationToken cancellationToken)
    {
        var feedbackEvent = new CustomerFeedbackReceivedEvent
        {
            FeedbackId = Guid.NewGuid(),
            CustomerName = request.CustomerName!,
            RecipeName = request.RecipeName!,
            Rating = request.Rating,
            Comments = request.Comments ?? string.Empty,
            ReceivedAt = DateTime.UtcNow
        };

        await publisher.PublishEvent(feedbackEvent, cancellationToken);

        return Accepted(new { feedbackEvent.FeedbackId });
    }
}

public class CustomerFeedbackRequest
{
    public string? CustomerName { get; set; }
    public string? RecipeName { get; set; }
    public int Rating { get; set; }
    public string? Comments { get; set; }
}
