using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Orders;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions;

/// <summary>
/// Holds step definitions that are reused across multiple feature files.
/// Each step text must only be bound once in the entire Reqnroll project.
/// </summary>
[Binding]
public class SharedSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps,
    DownstreamRequestSteps downstreamSteps)
{
    // ── Given: a pancake batch has been created ──
    // Used by: BreakfastOrder, AuditLogRetrieval, CompleteLifecycle, CrossFieldValidation,
    //          KitchenServiceFailure, OrderRetrieval, OutboxRetryExhaustion, RateLimiting, Telemetry

    [Given("a pancake batch has been created")]
    public async Task GivenAPancakeBatchHasBeenCreated()
    {
        await milkSteps.Retrieve();
        Track.That(() => milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        await eggsSteps.Retrieve();
        Track.That(() => eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        await flourSteps.Retrieve();
        Track.That(() => flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = milkSteps.MilkResponse.Milk,
            Eggs = eggsSteps.EggsResponse.Eggs,
            Flour = flourSteps.FlourResponse.Flour
        };
        await pancakeSteps.Send();

        Track.That(() => pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await pancakeSteps.ParseResponse();
        Track.That(() => pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => pancakeSteps.Response!.BatchId.Should().NotBeEmpty());
    }

    // ── Given: a valid order request ──
    // Used by: RateLimiting, Telemetry

    [Given("a valid order request")]
    public void GivenAValidOrderRequest()
    {
        orderSteps.Request = new TestOrderRequest
        {
            CustomerName = $"TestCustomer_{Random.Shared.NextInt64()}",
            TableNumber = 5,
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
    }

    // ── When: the order is submitted ──
    // Used by: CrossFieldValidation, Telemetry

    [When("the order is submitted")]
    public async Task WhenTheOrderIsSubmitted()
    {
        await orderSteps.Send();
    }

    // ── When: milk is requested from the milk endpoint ──
    // Used by: HeaderPropagation (CorrelationIdSteps), IngredientsSteps
    // NOTE: This step has different behaviour per feature — CorrelationIdSteps sends a correlation ID header,
    //       IngredientsSteps just sends a plain request. Since the step text is identical, we keep the
    //       simpler version here and let the CorrelationIdSteps use a distinct step text.
    //       (See CorrelationIdSteps for the renamed version.)

    [When("milk is requested from the milk endpoint")]
    public async Task WhenMilkIsRequestedFromTheMilkEndpoint()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Milk);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, appManager.RequestId);
        await appManager.Client.SendAsync(request);
    }

    // ── Then: the cow service should have received a milk request ──
    // Used by: PancakesCreation, WafflesCreation, AuditLogRetrieval, CompleteLifecycle

    [Then("the cow service should have received a milk request")]
    public void ThenTheCowServiceShouldHaveReceivedAMilkRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertCowServiceReceivedMilkRequest();
    }

    // ── Then: the kitchen service should have received a preparation request ──
    // Used by: BreakfastOrder, AuditLogRetrieval, CompleteLifecycle

    [Then("the kitchen service should have received a preparation request")]
    public void ThenTheKitchenServiceShouldHaveReceivedAPreparationRequest()
    {
        if (AppManager.Settings.RunAgainstExternalServiceUnderTest) return;
        downstreamSteps.AssertKitchenServiceReceivedPreparationRequest();
    }
}
