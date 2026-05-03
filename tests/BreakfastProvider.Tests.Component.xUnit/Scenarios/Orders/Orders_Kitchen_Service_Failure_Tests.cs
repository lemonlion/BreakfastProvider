using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

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
    public async Task Creating_an_order_when_kitchen_service_returns_error_should_still_create_the_order()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

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

        // And a valid order request for the created batch
        _orderSteps.Request.CustomerName = _customerName;
        _orderSteps.Request.TableNumber = 7;
        _orderSteps.Request.Items.Add(new TestOrderItemRequest
        {
            ItemType = OrderDefaults.PancakeItemType,
            BatchId = _pancakeSteps.Response!.BatchId,
            Quantity = 1
        });

        // And the kitchen service will return an error
        _orderSteps.AddHeader(FakeScenarioHeaders.KitchenService, FakeScenarios.KitchenBusy);

        // When the breakfast order is placed
        await _orderSteps.Send();

        // Then the order should still be created successfully
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _orderSteps.ParseResponse();
        Track.That(() => _orderSteps.Response!.CustomerName.Should().Be(_customerName));

        // And the order should be retrievable by its id
        await _getOrderSteps.Retrieve(_orderSteps.Response!.OrderId);
        Track.That(() => _getOrderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
    }
}
