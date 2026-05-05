using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Kitchen_Service_Failure_Feature : BaseFixture
{
    private readonly string _customerName = $"TestCustomer_{Random.Shared.NextInt64()}";

    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly PostOrderSteps _orderSteps;
    private readonly GetOrderSteps _getOrderSteps;

    public Orders__Kitchen_Service_Failure_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
        _getOrderSteps = Get<GetOrderSteps>();
    }

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted(),
            _ => The_pancake_response_should_be_successful());
    }

    private async Task Milk_is_retrieved()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task Eggs_are_retrieved()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task Flour_is_retrieved()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task A_pancake_request_is_submitted()
    {
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
    }

    private async Task The_pancake_response_should_be_successful()
    {
        _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _pancakeSteps.ParseResponse();
        _pancakeSteps.Response.Should().NotBeNull();
    }

    private async Task A_valid_order_request_for_the_created_batch()
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

    private async Task The_kitchen_service_will_return_an_error()
        => _orderSteps.AddHeader(FakeScenarioHeaders.KitchenService, FakeScenarios.KitchenBusy);

    #endregion

    #region When

    private async Task The_breakfast_order_is_placed()
        => await _orderSteps.Send();

    #endregion

    #region Then

    private async Task<CompositeStep> The_order_should_still_be_created_successfully()
    {
        return Sub.Steps(
            _ => The_order_response_http_status_should_be_created(),
            _ => The_order_response_should_be_valid_json(),
            _ => The_order_should_contain_the_customer_name());
    }

    private async Task The_order_response_http_status_should_be_created()
        => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_order_response_should_be_valid_json()
        => await _orderSteps.ParseResponse();

    private async Task The_order_should_contain_the_customer_name()
        => _orderSteps.Response!.CustomerName.Should().Be(_customerName);

    private async Task The_order_should_be_retrievable_by_its_id()
    {
        await _getOrderSteps.Retrieve(_orderSteps.Response!.OrderId);
        _getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion
}
