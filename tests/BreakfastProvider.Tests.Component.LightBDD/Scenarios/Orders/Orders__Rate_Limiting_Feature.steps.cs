using System.Net;
using BreakfastProvider.Api.Configuration;
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
public partial class Orders__Rate_Limiting_Feature : BaseFixture
{
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;

    private HttpResponseMessage? _firstResponse;
    private HttpResponseMessage? _secondResponse;

    public Orders__Rate_Limiting_Feature() : base(delayAppCreation: true)
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

    private async Task The_rate_limit_is_configured_to_allow_one_request_per_window()
        => EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.PermitLimit)}"] = "1",
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.WindowSeconds)}"] = "60"
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

    private async Task A_valid_order_request()
    {
        _orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"RateLimitTest_{Random.Shared.NextInt64()}",
            TableNumber = 1,
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
    }

    #endregion

    #region When

    private async Task The_order_is_submitted_twice_in_rapid_succession()
    {
        await _orderSteps.Send();
        _firstResponse = _orderSteps.ResponseMessage;

        // Second request with different customer name to avoid any dedup
        _orderSteps.Request.CustomerName = $"RateLimitTest2_{Random.Shared.NextInt64()}";
        await _orderSteps.Send();
        _secondResponse = _orderSteps.ResponseMessage;
    }

    #endregion

    #region Then

    private async Task The_first_request_should_succeed()
        => Track.That(() => _firstResponse!.StatusCode.Should().Be(HttpStatusCode.Created));

    private async Task The_second_request_should_be_rate_limited()
        => Track.That(() => _secondResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests));

    #endregion
}
