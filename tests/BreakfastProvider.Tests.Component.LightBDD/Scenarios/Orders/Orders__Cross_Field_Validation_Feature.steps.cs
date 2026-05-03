using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using Microsoft.AspNetCore.Mvc;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Cross_Field_Validation_Feature : BaseFixture
{
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;

    public Orders__Cross_Field_Validation_Feature() : base(delayAppCreation: true)
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

    #region Given

    private async Task The_maximum_items_per_order_is_configured_to_two()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(OrderConfig)}:{nameof(OrderConfig.MaxItemsPerOrder)}"] = "2"
        });

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => A_pancake_request_is_submitted_with_ingredients(),
            _ => The_pancake_batch_should_be_successful());
    }

    private async Task A_pancake_request_is_submitted_with_ingredients()
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
    }

    private async Task The_pancake_batch_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
    }

    private async Task An_order_request_with_three_items()
    {
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
    }

    private async Task An_order_request_with_two_items()
    {
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
    }

    #endregion

    #region When

    private async Task The_order_is_submitted()
        => await _orderSteps.Send();

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_indicate_a_validation_error()
    {
        return Sub.Steps(
            _ => The_order_response_status_should_be_bad_request());
    }

    private async Task The_order_response_status_should_be_bad_request()
        => Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));

    private async Task The_error_message_should_reference_the_item_limit()
    {
        var content = await _orderSteps.ResponseMessage!.Content.ReadAsStringAsync();
        var problemDetails = Json.Deserialize<ValidationProblemDetails>(content);
        var orderValidationErrors = problemDetails?.Errors.Values.SelectMany(v => v).ToList();
        Track.That(() => orderValidationErrors.Should().Contain(e => e.Contains("cannot contain more than 2 items")));
    }

    private async Task The_response_should_indicate_success()
        => Track.That(() => _orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

    #endregion
}
