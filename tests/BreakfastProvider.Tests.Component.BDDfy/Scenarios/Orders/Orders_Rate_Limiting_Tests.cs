using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Rate_Limiting_Tests : BaseFixture
{
    private GetMilkSteps _milkSteps = null!;
    private GetEggsSteps _eggsSteps = null!;
    private GetFlourSteps _flourSteps = null!;
    private PostPancakesSteps _pancakeSteps = null!;
    private PostOrderSteps _orderSteps = null!;

    public Orders_Rate_Limiting_Tests() : base(delayAppCreation: true)
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

    private HttpResponseMessage? _firstResponse;
    private HttpResponseMessage? _secondResponse;

    [Fact]
    public void Exceeding_rate_limit_should_return_too_many_requests()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        this.Given(x => x.The_rate_limit_is_configured_to_allow_one_request_per_window())
            .And(x => x.A_pancake_batch_has_been_created())
            .And(x => x.A_valid_order_request_is_prepared())
            .When(x => x.The_order_is_submitted_twice_in_rapid_succession())
            .Then(x => x.The_first_request_should_succeed_and_the_second_should_be_rate_limited())
            .BDDfy();
    }

    #region Steps

    private void The_rate_limit_is_configured_to_allow_one_request_per_window()
    {
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.PermitLimit)}"] = "1",
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.WindowSeconds)}"] = "60"
        });
    }

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
    }

    private void A_valid_order_request_is_prepared()
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

    private async Task The_order_is_submitted_twice_in_rapid_succession()
    {
        await _orderSteps.Send();
        _firstResponse = _orderSteps.ResponseMessage;

        _orderSteps.Request.CustomerName = $"RateLimitTest2_{Random.Shared.NextInt64()}";
        await _orderSteps.Send();
        _secondResponse = _orderSteps.ResponseMessage;
    }

    private void The_first_request_should_succeed_and_the_second_should_be_rate_limited()
    {
        _firstResponse!.StatusCode.Should().Be(HttpStatusCode.Created);
        _secondResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    #endregion
}
