using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Kitchen_Service_Failure_Tests : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetOrderSteps _getOrderSteps;

    public Orders_Kitchen_Service_Failure_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _getOrderSteps = Get<GetOrderSteps>();
    }

    [Fact]
    public void Creating_an_order_when_kitchen_service_returns_error_should_still_create_the_order()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.A_pancake_batch_has_been_created())
            .And(x => x.A_valid_order_request_for_the_created_batch())
            .And(x => x.The_kitchen_service_will_return_an_error())
            .When(x => x.The_breakfast_order_is_placed())
            .Then(x => x.The_order_should_still_be_created_successfully())
            .And(x => x.The_order_should_be_retrievable_by_its_id())
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
    }

    private void A_valid_order_request_for_the_created_batch()
    {
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });
    }

    private void The_kitchen_service_will_return_an_error()
    {
        _orderSteps.AddHeader(FakeScenarioHeaders.KitchenService, FakeScenarios.KitchenBusy);
    }

    private async Task The_breakfast_order_is_placed()
    {
        await _orderSteps.Send();
    }

    private async Task The_order_should_still_be_created_successfully()
    {
        _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _orderSteps.ParseResponse();
        _orderSteps.Response!.CustomerName.Should().Be(_customerName);
    }

    private async Task The_order_should_be_retrievable_by_its_id()
    {
        await _getOrderSteps.Retrieve(_orderSteps.Response!.OrderId);
        _getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
