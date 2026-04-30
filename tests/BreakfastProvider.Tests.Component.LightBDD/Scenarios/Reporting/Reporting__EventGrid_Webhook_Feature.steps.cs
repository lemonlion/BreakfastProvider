using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reporting__EventGrid_Webhook_Feature : BaseFixture
{
    private readonly GraphQlReportingSteps _graphQlSteps;

    private readonly Guid _deliveryId = Guid.NewGuid();
    private HttpResponseMessage? _webhookResponse;

    public Reporting__EventGrid_Webhook_Feature()
    {
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    #region Given

    private async Task<CompositeStep> An_ingredient_delivery_event_has_been_received_via_eventgrid_webhook()
    {
        return Sub.Steps(
            _ => An_ingredient_delivery_event_is_posted_to_the_webhook(),
            _ => The_webhook_response_should_be_successful());
    }

    private async Task An_ingredient_delivery_event_is_posted_to_the_webhook()
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
        _webhookResponse = await Client.SendAsync(request);
    }

    private async Task The_webhook_response_should_be_successful()
        => _webhookResponse!.StatusCode.Should().Be(HttpStatusCode.OK);

    #endregion

    #region When

    private async Task The_ingredient_shipments_are_queried_via_graphql()
        => await _graphQlSteps.QueryIngredientShipments();

    #endregion

    #region Then

    private async Task<CompositeStep> The_graphql_response_should_contain_the_ingredient_shipment()
    {
        return Sub.Steps(
            _ => The_ingredient_shipments_response_should_be_successful(),
            _ => The_ingredient_shipments_response_should_be_valid_json(),
            _ => The_ingredient_shipments_should_contain_the_delivery());
    }

    private async Task The_ingredient_shipments_response_should_be_successful()
        => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_ingredient_shipments_response_should_be_valid_json()
        => await _graphQlSteps.ParseIngredientShipmentsResponse();

    private async Task The_ingredient_shipments_should_contain_the_delivery()
    {
        _graphQlSteps.IngredientShipments.Should().Contain(s =>
            s.DeliveryId == _deliveryId &&
            s.IngredientName == "Milk" &&
            s.Quantity == 50.0m);
    }

    #endregion
}
