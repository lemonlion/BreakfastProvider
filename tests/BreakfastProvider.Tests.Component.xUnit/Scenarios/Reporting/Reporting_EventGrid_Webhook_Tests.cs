using System.Net;
using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Reporting;

public class Reporting_EventGrid_Webhook_Tests : BaseFixture
{
    private readonly GraphQlReportingSteps _graphQlSteps;
    private readonly Guid _deliveryId = Guid.NewGuid();

    public Reporting_EventGrid_Webhook_Tests()
    {
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Ingredient_shipments_should_be_recorded_when_delivered_via_eventgrid_webhook()
    {
        // Given an ingredient delivery event has been received via EventGrid webhook
        var eventGridPayload = new[]
        {
            new
            {
                id = Guid.NewGuid().ToString(),
                eventType = "IngredientDeliveryEvent",
                subject = "supply-chain/deliveries",
                dataVersion = "1.0",
                eventTime = DateTime.UtcNow.ToString("O"),
                data = new
                {
                    deliveryId = _deliveryId,
                    ingredientName = "Milk",
                    quantity = 50.0m,
                    deliveredAt = DateTime.UtcNow
                }
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Endpoints.EventGridWebhook)
        {
            Content = JsonContent.Create(eventGridPayload)
        };
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        var webhookResponse = await Client.SendAsync(request);
        Track.That(() => webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK));

        // When the ingredient shipments are queried via GraphQL
        await _graphQlSteps.QueryIngredientShipments();

        // Then the response should contain the ingredient shipment
        Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _graphQlSteps.ParseIngredientShipmentsResponse();
        Track.That(() => _graphQlSteps.IngredientShipments.Should().Contain(s =>
            s.DeliveryId == _deliveryId &&
            s.IngredientName == "Milk" &&
            s.Quantity == 50.0m));
    }
}
