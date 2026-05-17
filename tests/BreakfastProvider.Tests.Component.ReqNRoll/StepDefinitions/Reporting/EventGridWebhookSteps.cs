using System.Net;
using System.Net.Http.Json;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Reporting;

[Binding]
public class EventGridWebhookSteps(
    AppManager appManager,
    GraphQlReportingSteps graphQlSteps)
{
    private readonly Guid _deliveryId = Guid.NewGuid();

    [Given("an ingredient delivery event has been received via eventgrid webhook")]
    public async Task GivenAnIngredientDeliveryEventHasBeenReceivedViaEventgridWebhook()
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
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        var response = await appManager.Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [When("the ingredient shipments are queried via graphql")]
    public async Task WhenTheIngredientShipmentsAreQueriedViaGraphql()
    {
        await graphQlSteps.QueryIngredientShipments();
    }

    [Then("the graphql response should contain the ingredient shipment record")]
    public async Task ThenTheGraphqlResponseShouldContainTheIngredientShipmentRecord()
    {
        graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await graphQlSteps.ParseIngredientShipmentsResponse();
        graphQlSteps.IngredientShipments.Should().Contain(s =>
            s.DeliveryId == _deliveryId &&
            s.IngredientName == "Milk" &&
            s.Quantity == 50.0m);
    }
}
