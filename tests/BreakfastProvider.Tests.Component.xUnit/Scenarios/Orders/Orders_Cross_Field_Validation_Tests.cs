using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;
using Microsoft.AspNetCore.Mvc;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

public class Orders_Cross_Field_Validation_Tests : BaseFixture
{
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;

    public Orders_Cross_Field_Validation_Tests() : base(delayAppCreation: true)
    {
    }

    private void EnsureAppCreated(Dictionary<string, string?> overrides)
    {
        CreateAppAndClient(overrides);
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _orderSteps = Get<PostOrderSteps>();
    }

    private async Task CreatePancakeBatch()
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
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
    }

    [Fact]
    public async Task Order_exceeding_maximum_items_per_order_should_be_rejected()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(OrderConfig)}:{nameof(OrderConfig.MaxItemsPerOrder)}"] = "2"
        });

        await CreatePancakeBatch();

        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"CrossFieldTest_{Random.Shared.NextInt64()}",
            TableNumber = 1,
            Items =
            [
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 }
            ]
        };

        // When
        await _orderSteps.Send();

        // Then
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));

        var content = await _orderSteps.ResponseMessage!.Content.ReadAsStringAsync();
        var problemDetails = Json.Deserialize<ValidationProblemDetails>(content);
        var orderValidationErrors = problemDetails?.Errors.Values.SelectMany(v => v).ToList();
        Track.That(() => orderValidationErrors.Should().Contain(e => e.Contains("cannot contain more than 2 items")));
    }

    [Fact]
    public async Task Order_at_maximum_items_per_order_should_be_accepted()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(OrderConfig)}:{nameof(OrderConfig.MaxItemsPerOrder)}"] = "2"
        });

        await CreatePancakeBatch();

        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"CrossFieldTest_{Random.Shared.NextInt64()}",
            TableNumber = 1,
            Items =
            [
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 },
                new TestOrderItemRequest { ItemType = OrderDefaults.PancakeItemType, BatchId = _pancakeSteps.Response!.BatchId, Quantity = 1 }
            ]
        };

        // When
        await _orderSteps.Send();

        // Then
        Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
    }
}
