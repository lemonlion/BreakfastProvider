namespace BreakfastProvider.Tests.Component.Shared.Fakes.EventGrid;

public interface IPublishedEventStore
{
    Task<IReadOnlyList<T>> GetPublishedEventsAsync<T>() where T : class;
}
