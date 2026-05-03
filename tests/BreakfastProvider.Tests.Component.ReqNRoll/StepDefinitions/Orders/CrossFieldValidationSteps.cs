using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class CrossFieldValidationSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps)
{
    [Given("the maximum items per order is configured to two")]
    public void GivenTheMaximumItemsPerOrderIsConfiguredToTwo()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(OrderConfig)}:{nameof(OrderConfig.MaxItemsPerOrder)}"] = "2"
        });
    }

    [Given("an order request with three items")]
    public void GivenAnOrderRequestWithThreeItems()
    {
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"CrossFieldTest_{Random.Shared.NextInt64()}",
            TableNumber = 1,
            Items =
            [
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 }
            ]
        };
    }

    [Given("an order request with two items")]
    public void GivenAnOrderRequestWithTwoItems()
    {
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"CrossFieldTest_{Random.Shared.NextInt64()}",
            TableNumber = 1,
            Items =
            [
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = pancakeSteps.Response!.BatchId, Quantity = 1 }
            ]
        };
    }


    [Then("the response should indicate a validation error")]
    public void ThenTheResponseShouldIndicateAValidationError()
    {
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }

    [Then("the error message should reference the item limit")]
    public async Task ThenTheErrorMessageShouldReferenceTheItemLimit()
    {
        var orderValidationErrorResponseBody = await orderSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => orderValidationErrorResponseBody.Should().Contain("Items"));
    }

    [Then("the response should indicate success")]
    public void ThenTheResponseShouldIndicateSuccess()
    {
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
    }
}
