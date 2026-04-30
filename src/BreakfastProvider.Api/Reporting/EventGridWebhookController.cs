using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Api.Reporting;

/// <summary>
/// Receives EventGrid events delivered via webhook subscription.
/// In production, Azure EventGrid pushes events to this endpoint.
/// In component tests, events are POSTed directly to simulate delivery.
/// </summary>
[ApiController]
[Route("webhooks/eventgrid")]
public class EventGridWebhookController(
    IReportingIngester ingester,
    ILogger<EventGridWebhookController> logger) : ControllerBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Processes an array of EventGrid-schema events. Supports subscription
    /// validation and <c>IngredientDeliveryEvent</c> processing.
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ReceiveEvents([FromBody] JsonElement body, CancellationToken cancellationToken)
    {
        var events = body.ValueKind == JsonValueKind.Array
            ? body.EnumerateArray().ToList()
            : [body];

        foreach (var eventElement in events)
        {
            var eventType = eventElement.TryGetProperty("eventType", out var et)
                ? et.GetString()
                : null;

            if (eventType == "Microsoft.EventGrid.SubscriptionValidationEvent")
            {
                var validationCode = eventElement
                    .GetProperty("data")
                    .GetProperty("validationCode")
                    .GetString();

                return Ok(new { validationResponse = validationCode });
            }

            if (string.Equals(eventType, "IngredientDeliveryEvent", StringComparison.OrdinalIgnoreCase))
            {
                await ProcessIngredientDelivery(eventElement, cancellationToken);
            }
            else
            {
                logger.LogDebug("Ignoring unhandled EventGrid event type: {EventType}", eventType);
            }
        }

        return Ok();
    }

    private async Task ProcessIngredientDelivery(JsonElement eventElement, CancellationToken cancellationToken)
    {
        if (!eventElement.TryGetProperty("data", out var data))
            return;

        var delivery = data.Deserialize<IngredientDeliveryData>(JsonOptions);
        if (delivery is null) return;

        await ingester.IngestIngredientShipmentAsync(
            delivery.DeliveryId,
            delivery.IngredientName,
            delivery.Quantity,
            delivery.DeliveredAt,
            cancellationToken);

        logger.LogInformation("Processed ingredient delivery {DeliveryId} for {Ingredient}",
            delivery.DeliveryId, delivery.IngredientName);
    }

    private class IngredientDeliveryData
    {
        public Guid DeliveryId { get; set; }
        public string IngredientName { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public DateTime DeliveredAt { get; set; }
    }
}
