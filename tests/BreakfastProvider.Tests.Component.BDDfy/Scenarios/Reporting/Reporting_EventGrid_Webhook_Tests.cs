using System.Net;
using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Reporting;

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
    public void Ingredient_shipments_should_be_recorded_when_delivered_via_eventgrid_webhook()
    {
        this.Given(x => x.An_ingredient_delivery_event_has_been_received_via_eventgrid_webhook())
            .When(x => x.The_ingredient_shipments_are_queried_via_graphql())
            .Then(x => x.The_response_should_contain_the_ingredient_shipment())
            .BDDfy();
    }

    #region Steps

    private async Task An_ingredient_delivery_event_has_been_received_via_eventgrid_webhook()
    {
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
        webhookResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task The_ingredient_shipments_are_queried_via_graphql()
    {
        await _graphQlSteps.QueryIngredientShipments();
    }

    private async Task The_response_should_contain_the_ingredient_shipment()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseIngredientShipmentsResponse();
        _graphQlSteps.IngredientShipments.Should().Contain(s =>
            s.DeliveryId == _deliveryId &&
            s.IngredientName == "Milk" &&
            s.Quantity == 50.0m);
    }

    #endregion
}
