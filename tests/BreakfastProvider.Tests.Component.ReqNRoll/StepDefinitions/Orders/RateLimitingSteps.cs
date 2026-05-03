using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class RateLimitingSteps(
    AppManager appManager,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps)
{
    private HttpResponseMessage? _firstResponse;
    private HttpResponseMessage? _secondResponse;

    [Given("the rate limit is configured to allow one request per window")]
    public void GivenTheRateLimitIsConfiguredToAllowOneRequestPerWindow()
    {
        appManager.CreateAppWithOverrides(new Dictionary<string, string?>
        {
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.PermitLimit)}"] = "1",
            [$"{nameof(RateLimitConfig)}:{nameof(RateLimitConfig.WindowSeconds)}"] = "60"
        });
    }

    [When("the order is submitted twice in rapid succession")]
    public async Task WhenTheOrderIsSubmittedTwiceInRapidSuccession()
    {
        await orderSteps.Send();
        _firstResponse = orderSteps.ResponseMessage;

        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"RateLimitTest2_{Random.Shared.NextInt64()}",
            TableNumber = 2,
            Items =
            [
                new TestOrderItemRequest
                {
                    ItemType = OrderDefaults.PancakeItemType,
                    BatchId = pancakeSteps.Response!.BatchId,
                    Quantity = 1
                }
            ]
        };
        await orderSteps.Send();
        _secondResponse = orderSteps.ResponseMessage;
    }

    [Then("the first request should succeed")]
    public void ThenTheFirstRequestShouldSucceed()
    {
        Track.That(() => _firstResponse!.StatusCode.Should().Be(HttpStatusCode.Created));
    }

    [Then("the second request should be rate limited")]
    public void ThenTheSecondRequestShouldBeRateLimited()
    {
        Track.That(() => _secondResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests));
    }
}
