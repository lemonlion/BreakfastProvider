using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class PaginationSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    ListOrdersSteps listSteps)
{
    private int _createdOrderCount;

    [BeforeScenario("Pagination", Order = 50)]
    public void SetupIsolatedApp()
    {
        appManager.CreateAppWithOverrides(additionalServices: _ => { });
    }

    [Given("multiple orders have been created")]
    public async Task GivenMultipleOrdersHaveBeenCreated()
    {
        await milkSteps.Retrieve();
        await eggsSteps.Retrieve();
        await flourSteps.Retrieve();

        pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = milkSteps.MilkResponse.Milk,
            Eggs = eggsSteps.EggsResponse.Eggs,
            Flour = flourSteps.FlourResponse.Flour
        };
        await pancakeSteps.Send();
        Track.That(() => pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await pancakeSteps.ParseResponse();

        for (var i = 0; i < 2; i++)
        {
            orderSteps.Request = new TestOrderRequest
            {
                CustomerName = $"PaginationTest_{Random.Shared.NextInt64()}",
                TableNumber = i + 1,
                Items =
                [
                    new TestOrderItemRequest
                    {
                        ItemType = OrderDefaults.PancakeItemType,
                        BatchId = pancakeSteps.Response!.BatchId,
                        Quantity = 1
                    }
                ]
            };
            await orderSteps.Send();
            Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        }

        _createdOrderCount = 2;
    }

    [When("orders are listed with default pagination")]
    public async Task WhenOrdersAreListedWithDefaultPagination()
    {
        await listSteps.Retrieve();
    }

    [When(@"orders are listed with page (\d+) and page size (\d+)")]
    public async Task WhenOrdersAreListedWithPageAndPageSize(int page, int pageSize)
    {
        await listSteps.Retrieve(page, pageSize);
    }

    [Then("the paginated response should contain the orders")]
    public async Task ThenThePaginatedResponseShouldContainTheOrders()
    {
        Track.That(() => listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await listSteps.ParseResponse();
        Track.That(() => listSteps.Response!.Items.Should().HaveCountGreaterThanOrEqualTo(_createdOrderCount));
    }

    [Then("the paginated response should have correct page metadata")]
    public async Task ThenThePaginatedResponseShouldHaveCorrectPageMetadata()
    {
        await listSteps.ParseResponse();
        Track.That(() => listSteps.Response!.Page.Should().BeGreaterThanOrEqualTo(1));
        Track.That(() => listSteps.Response!.TotalCount.Should().BeGreaterThanOrEqualTo(_createdOrderCount));
    }

    [Then("the paginated response should be empty")]
    public async Task ThenThePaginatedResponseShouldBeEmpty()
    {
        Track.That(() => listSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await listSteps.ParseResponse();
    }
}
