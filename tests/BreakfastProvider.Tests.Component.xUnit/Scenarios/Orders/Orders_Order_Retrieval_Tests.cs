using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

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

    [Fact]
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
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());

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
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _orderSteps.ParseResponse();
        Track.That(() => _orderSteps.Response.Should().NotBeNull());
        Track.That(() => _orderSteps.Response!.OrderId.Should().NotBeEmpty());

        // When the order is retrieved by id
        await _retrievalSteps.Retrieve(_orderSteps.Response!.OrderId);

        // Then the retrieved order should match the created order
        Track.That(() => _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _retrievalSteps.ParseResponse();
        Track.That(() => _retrievalSteps.Response!.OrderId.Should().Be(_orderSteps.Response!.OrderId));
        Track.That(() => _retrievalSteps.Response!.CustomerName.Should().Be(_customerName));
        Track.That(() => _retrievalSteps.Response!.Items.Should().HaveCount(1));

        // And the cow service should have received a milk request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();

        // And the kitchen service should have received a preparation request
        if (!Settings.RunAgainstExternalServiceUnderTest)
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }

    [Fact]
    public async Task Retrieving_a_non_existent_order_should_return_not_found()
    {
        // Given a non-existent order id
        var nonExistentOrderId = Guid.NewGuid();

        // When the order is retrieved by id
        await _retrievalSteps.Retrieve(nonExistentOrderId);

        // Then the response should be not found
        Track.That(() => _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }
}
