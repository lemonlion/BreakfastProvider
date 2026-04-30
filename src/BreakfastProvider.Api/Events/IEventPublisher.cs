namespace BreakfastProvider.Api.Events;

public interface IEventPublisher<in T> where T : class
{
    Task PublishAsync(T @event, CancellationToken cancellationToken = default);
}
