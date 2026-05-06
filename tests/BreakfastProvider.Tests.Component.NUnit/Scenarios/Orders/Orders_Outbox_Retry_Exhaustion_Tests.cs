using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Orders;

public class Orders_Outbox_Retry_Exhaustion_Tests : BaseFixture
{
    private const string TestDestination = "TestRetryExhaustion";
    private OutboxSteps _outboxSteps = null!;
    private ICosmosRepository<OutboxMessage> _outboxRepository = null!;

    public Orders_Outbox_Retry_Exhaustion_Tests() : base(delayAppCreation: true)
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        CreateAppAndClient(
            configOverrides: new Dictionary<string, string?>
            {
                ["OutboxConfig:PollingIntervalSeconds"] = "1",
                ["OutboxConfig:MaxRetryCount"] = "2"
            },
            additionalServices: services =>
            {
                services.RemoveAll<IOutboxDispatcher>();
                services.AddSingleton<IOutboxDispatcher>(new FailingOutboxDispatcher());
            });

        _outboxRepository = AppFactory.Services.GetRequiredService<ICosmosRepository<OutboxMessage>>();
        _outboxSteps = new OutboxSteps(_outboxRepository);
    }

    [Test]
    public async Task Outbox_message_should_transition_to_failed_after_exhausting_retries()
    {
        if (Settings.RunAgainstExternalServiceUnderTest) return;

        // Given a pending outbox message with a test-specific destination
        // (the static factory's processor will skip it — no matching dispatcher)
        var message = new OutboxMessage
        {
            PartitionKey = "outbox-retry-test",
            EventType = EventTypes.OrderCreated,
            Destination = TestDestination,
            Payload = """{"CustomerName":"RetryTest","OrderId":"00000000-0000-0000-0000-000000000001"}""",
            Status = OutboxMessageStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        await _outboxRepository.CreateAsync(message, message.PartitionKey);

        // Then the outbox message should transition to failed after exhausting retries
        const int maxRetries = 120;
        var retryDelay = TimeSpan.FromMilliseconds(500);

        for (var i = 0; i < maxRetries; i++)
        {
            await _outboxSteps.LoadOutboxMessages();
            if (_outboxSteps.OutboxMessages!.Any(m =>
                    m.EventType == EventTypes.OrderCreated &&
                    m.Status == OutboxStatuses.Failed))
                return;
            await Task.Delay(retryDelay);
        }

        await _outboxSteps.LoadOutboxMessages();
        _outboxSteps.OutboxMessages.Should().Contain(m =>
                m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed,
            "the outbox message should have transitioned to Failed after exhausting retries");
    }

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => TestDestination;

        public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
