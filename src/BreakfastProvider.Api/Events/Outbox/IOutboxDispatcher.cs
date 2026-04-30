using BreakfastProvider.Api.Storage;

namespace BreakfastProvider.Api.Events.Outbox;

public interface IOutboxDispatcher
{
    string Destination { get; }
    Task DispatchAsync(OutboxMessage message, CancellationToken cancellationToken = default);
}
