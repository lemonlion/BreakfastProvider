using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class OutboxRetryExhaustionSteps(AppManager appManager)
{
    private const string TestDestination = "TestRetryExhaustion";

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

    [Given("a pending outbox message with a test-specific destination")]
    public async Task GivenAPendingOutboxMessageWithATestSpecificDestination()
    {
        var outboxRepo = appManager.AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>();
        var message = new OutboxMessage
        {
            PartitionKey = "outbox-retry-test",
            EventType = EventTypes.OrderCreated,
            Destination = TestDestination,
            Payload = """{"CustomerName":"RetryTest","OrderId":"00000000-0000-0000-0000-000000000001"}""",
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await outboxRepo.CreateAsync(message, message.PartitionKey);
    }

    [Then("the outbox message should be in a failed state")]
    public async Task ThenTheOutboxMessageShouldBeInAFailedState()
    {
        var outboxRepo = appManager.AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>();
        var outboxSteps = new OutboxSteps(outboxRepo);

        const int maxRetries = 120;
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
        outboxSteps.OutboxMessages.Should().Contain(m =>
                m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed,
            "the outbox message should have transitioned to Failed after exhausting retries");
    }

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => TestDestination;

        public Task DispatchAsync(OutboxMessage message, CancellationToken ct = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
