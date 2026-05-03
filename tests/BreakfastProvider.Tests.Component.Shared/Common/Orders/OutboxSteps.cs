using BreakfastProvider.Api.Storage;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Events;

namespace BreakfastProvider.Tests.Component.Shared.Common.Orders;

public class OutboxSteps(ICosmosRepository<OutboxMessage> outboxRepository)
{
    public IReadOnlyList<TestOutboxMessage>? OutboxMessages { get; private set; }

    public async Task LoadOutboxMessages()
    {
        var documents = await outboxRepository.QueryAsync(_ => true);
        OutboxMessages = documents.Select(d => new TestOutboxMessage
        {
            Id = d.Id,
            EventType = d.EventType,
            Destination = d.Destination,
            Payload = d.Payload,
            Status = d.Status,
            CreatedAt = d.CreatedAt,
            ProcessedAt = d.ProcessedAt,
            RetryCount = d.RetryCount,
            ErrorMessage = d.ErrorMessage
        }).ToList();
    }

    public void AssertOutboxContainsMessageForEventType(string eventType)
    {
        Track.That(() => OutboxMessages.Should().Contain(m => m.EventType == eventType,
            $"an outbox message should exist for event type '{eventType}'"));
    }

    public void AssertOutboxMessageWasProcessed(string eventType)
    {
        Track.That(() => OutboxMessages.Should().Contain(m =>
            m.EventType == eventType && m.Status == OutboxStatuses.Processed,
            $"an outbox message for '{eventType}' should have been processed"));
    }
}
