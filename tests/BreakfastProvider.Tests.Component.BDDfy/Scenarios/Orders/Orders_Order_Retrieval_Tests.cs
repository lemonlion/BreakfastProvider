using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

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
    public void Previously_created_order_should_be_retrievable_by_id()
    {
        this.Given(x => x.A_pancake_batch_has_been_created())
            .And(x => x.An_order_has_been_created_for_the_batch())
            .When(x => x.The_order_is_retrieved_by_id())
            .Then(x => x.The_retrieved_order_should_match_the_created_order())
            .And(x => x.The_downstream_services_should_have_received_requests())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_a_non_existent_order_should_return_not_found()
    {
        this.Given(x => x.A_non_existent_order_id())
            .When(x => x.The_non_existent_order_is_retrieved())
            .Then(x => x.The_response_should_be_not_found())
            .BDDfy();
    }

    #region Steps

    private async Task A_pancake_batch_has_been_created()
    {
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
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
        _pancakeSteps.Response!.BatchId.Should().NotBeEmpty();
    }

    private async Task An_order_has_been_created_for_the_batch()
    {
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
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response.Should().NotBeNull();
        _orderSteps.Response!.OrderId.Should().NotBeEmpty();
    }

    private async Task The_order_is_retrieved_by_id()
    {
        await _retrievalSteps.Retrieve(_orderSteps.Response!.OrderId);
    }

    private async Task The_retrieved_order_should_match_the_created_order()
    {
        _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _retrievalSteps.ParseResponse();
        _retrievalSteps.Response!.OrderId.Should().Be(_orderSteps.Response!.OrderId);
        _retrievalSteps.Response!.CustomerName.Should().Be(_customerName);
        _retrievalSteps.Response!.Items.Should().HaveCount(1);
    }

    private void The_downstream_services_should_have_received_requests()
    {
        if (!Settings.RunAgainstExternalServiceUnderTest)
        {
            _downstreamSteps.AssertCowServiceReceivedMilkRequest();
            _downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
        }
    }

    private Guid _nonExistentOrderId;

    private void A_non_existent_order_id()
    {
        _nonExistentOrderId = Guid.NewGuid();
    }

    private async Task The_non_existent_order_is_retrieved()
    {
        await _retrievalSteps.Retrieve(_nonExistentOrderId);
    }

    private void The_response_should_be_not_found()
    {
        _retrievalSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
