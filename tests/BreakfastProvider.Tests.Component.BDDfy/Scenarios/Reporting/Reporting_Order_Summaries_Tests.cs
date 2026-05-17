using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Reporting;

public class Reporting_Order_Summaries_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";
    private Guid _orderId;

    public Reporting_Order_Summaries_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Order_summaries_should_contain_ingested_order_data()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .And(x => x.A_breakfast_order_has_been_placed_for_the_batch())
            .When(x => x.The_order_summaries_are_queried_via_graphql())
            .Then(x => x.The_response_should_contain_the_ingested_order_summary())
            .BDDfy();
    }

    [Fact]
    public void Order_summaries_should_return_an_empty_list_when_no_orders_exist()
    {
        this.When(x => x.The_order_summaries_are_queried_via_graphql())
            .Then(x => x.The_response_should_not_contain_the_test_order())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_has_been_created()
    {
        await _milkSteps.Retrieve();
        _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _eggsSteps.Retrieve();
        _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _flourSteps.Retrieve();
        _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

    private async Task A_breakfast_order_has_been_placed_for_the_batch()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 7,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = _pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await _orderSteps.Send();
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response.Should().NotBeNull();
        _orderId = _orderSteps.Response!.OrderId;
        _orderId.Should().NotBeEmpty();
    }

    private async Task The_order_summaries_are_queried_via_graphql()
    {
        await _graphQlSteps.QueryOrderSummaries();
    }

    private async Task The_response_should_contain_the_ingested_order_summary()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseOrderSummariesResponse();
        _graphQlSteps.OrderSummaries.Should().Contain(o =>
            o.OrderId == _orderId &&
            o.CustomerName == _customerName &&
            o.ItemCount == 1 &&
            o.TableNumber == 7);
    }

    private async Task The_response_should_not_contain_the_test_order()
    {
        _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _graphQlSteps.ParseOrderSummariesResponse();
        _graphQlSteps.OrderSummaries.Should().NotContain(o => o.OrderId == _orderId);
    }

    #endregion
}
