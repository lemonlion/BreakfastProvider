using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Orders;

public class Orders_Order_Retrieval_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetOrderSteps _retrievalSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Orders_Order_Retrieval_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _retrievalSteps = Get<GetOrderSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Previously_created_order_should_be_retrievable_by_id()
    {
        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        await _eggsSteps.Retrieve();
        await _flourSteps.Retrieve();

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

        // And an order has been created for the batch
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = _customerName,
            TableNumber = 3,
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
        await _orderSteps.Response!.OrderId.Should().NotBeEqualTo(Guid.Empty);

        // When the order is retrieved by id
        await _retrievalSteps.Retrieve(_orderSteps.Response!.OrderId);

        // Then the retrieved order should match the created order
        await _retrievalSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _retrievalSteps.ParseResponse();
        await _retrievalSteps.Response!.OrderId.Should().BeEqualTo(_orderSteps.Response!.OrderId);
        await _retrievalSteps.Response!.CustomerName.Should().BeEqualTo(_customerName);
        await _retrievalSteps.Response!.Items.Should().HaveCount(1);

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();

        // And the kitchen service should have received a preparation request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }

    [Test]
    public async Task Retrieving_a_non_existent_order_should_return_not_found()
    {
        // Given a non-existent order id
        var nonExistentOrderId = Guid.NewGuid();

        // When the order is retrieved by id
        await _retrievalSteps.Retrieve(nonExistentOrderId);

        // Then the response should be not found
        await _retrievalSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }
}
