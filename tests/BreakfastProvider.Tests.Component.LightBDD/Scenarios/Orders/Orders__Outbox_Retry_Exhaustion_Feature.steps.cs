using BreakfastProvider.Api.Events.Outbox;
using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Outbox_Retry_Exhaustion_Feature : BaseFixture
{
    private const string TestDestination = "TestRetryExhaustion";
    private OutboxSteps _outboxSteps = null!;
    private ICosmosRepository<OutboxMessage> _outboxRepository = null!;

    public Orders__Outbox_Retry_Exhaustion_Feature() : base(delayAppCreation: true)
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

    #region Given

    private async Task A_pending_outbox_message_with_a_test_specific_destination()
    {
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
    }

    #endregion

    #region Then

    private async Task The_outbox_message_should_transition_to_failed()
    {
        const int maxRetries = 120;
        var retryDelay = TimeSpan.FromMilliseconds(500);

        for (var i = 0; i < maxRetries; i++)
        {
            await _outboxSteps.LoadOutboxMessages();
            if (_outboxSteps.OutboxMessages!.Any(m =>
                    m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed))
                return;
            await Task.Delay(retryDelay);
        }

        await _outboxSteps.LoadOutboxMessages();
        _outboxSteps.OutboxMessages.Should().Contain(m =>
                m.EventType == EventTypes.OrderCreated && m.Status == OutboxStatuses.Failed,
            "the outbox message should have transitioned to Failed after exhausting retries");
    }

    #endregion

    private class FailingOutboxDispatcher : IOutboxDispatcher
    {
        public string Destination => TestDestination;

        public Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Simulated dispatch failure for testing retry exhaustion.");
    }
}
