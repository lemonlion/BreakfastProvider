using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class OutboxRetryExhaustionSteps(
    AppManager appManager,
    GetMilkSteps milkSteps,
    GetEggsSteps eggsSteps,
    GetFlourSteps flourSteps,
    PostPancakesSteps pancakeSteps,
    PostOrderSteps orderSteps)
{
    [Given("the outbox processor is configured with a failing dispatcher")]
    public void GivenTheOutboxProcessorIsConfiguredWithAFailingDispatcher()
    {
        appManager.CreateAppWithOverrides(
            new Dictionary<string, string?>
            {
                [$"{nameof(OutboxConfig)}:{nameof(OutboxConfig.PollingIntervalSeconds)}"] = "1",
                [$"{nameof(OutboxConfig)}:{nameof(OutboxConfig.MaxRetryCount)}"] = "2"
            },
            services =>
            {
                services.RemoveAll<IOutboxDispatcher>();
                services.AddSingleton<IOutboxDispatcher>(new FailingOutboxDispatcher());
            });
    }

    [When("the order is submitted and retries are exhausted")]
    public async Task WhenTheOrderIsSubmittedAndRetriesAreExhausted()
    {
        await orderSteps.Send();
        Track.That(() => orderSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
    }

    [Then("the outbox message should be in a failed state")]
    public async Task ThenTheOutboxMessageShouldBeInAFailedState()
    {
        var outboxRepo = appManager.AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>();
        var outboxSteps = new OutboxSteps(outboxRepo);

        const int maxRetries = 60;
        var retryDelay = TimeSpan.FromMilliseconds(500);

        for (var i = 0; i < maxRetries; i++)
        {
            await outboxSteps.LoadOutboxMessages();
            if (outboxSteps.OutboxMessages!.Any(m =>
                    m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed))
                return;
            await Task.Delay(retryDelay);
        }

        await outboxSteps.LoadOutboxMessages();
        Track.That(() => outboxSteps.OutboxMessages.Should().Contain(m =>
                m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed,
            "the outbox message should have transitioned to Failed after exhausting retries"));
    }

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => OutboxDestinations.EventGrid;

        public Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
