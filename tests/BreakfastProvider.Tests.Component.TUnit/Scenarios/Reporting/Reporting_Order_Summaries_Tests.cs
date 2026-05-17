using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Reporting;

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

    [Test]
    [HappyPath]
    public async Task Order_summaries_should_contain_ingested_order_data()
    {
        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        await _milkSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _eggsSteps.Retrieve();
        await _eggsSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _flourSteps.Retrieve();
        await _flourSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        await _pancakeSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        await _pancakeSteps.Response.Should().NotBeNull();
        await _pancakeSteps.Response!.BatchId.Should().NotBeEqualTo(Guid.Empty);

        // And a breakfast order has been placed for the batch
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
        await _orderSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        await _orderSteps.Response.Should().NotBeNull();
        _orderId = _orderSteps.Response!.OrderId;
        await _orderId.Should().NotBeEqualTo(Guid.Empty);

        // When the order summaries are queried via GraphQL
        await _graphQlSteps.QueryOrderSummaries();

        // Then the response should contain the ingested order summary
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParseOrderSummariesResponse();
        await _graphQlSteps.OrderSummaries.Should().Contain(o =>
            o.OrderId == _orderId &&
            o.CustomerName == _customerName &&
            o.ItemCount == 1 &&
            o.TableNumber == 7);
    }

    [Test]
    public async Task Order_summaries_should_return_an_empty_list_when_no_orders_exist()
    {
        // When the order summaries are queried via GraphQL
        await _graphQlSteps.QueryOrderSummaries();

        // Then the response should be successful and not contain the test order
        await _graphQlSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _graphQlSteps.ParseOrderSummariesResponse();
        await _graphQlSteps.OrderSummaries.Should().All(o => o.OrderId != _orderId);
    }
}
