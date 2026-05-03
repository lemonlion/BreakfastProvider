using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Orders;

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

    [Fact]
    public async Task Exceeding_rate_limit_should_return_too_many_requests()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given the rate limit is configured to allow one request per window
        EnsureAppCreated(new Dictionary<string, string?>
        {
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.PermitLimit)}"] = "1",
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.WindowSeconds)}"] = "60"
        });

        // And a pancake batch has been created
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

        // And a valid order request
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

        // When the order is submitted twice in rapid succession
        await _orderSteps.Send();
        var firstResponse = _orderSteps.ResponseMessage;

        _orderSteps.Request.CustomerName = $"RateLimitTest2_{Random.Shared.NextInt64()}";
        await _orderSteps.Send();
        var secondResponse = _orderSteps.ResponseMessage;

        // Then
        Track.That(() => firstResponse!.StatusCode.Should().Be(HttpStatusCode.Created));
        Track.That(() => secondResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests));
    }
}
