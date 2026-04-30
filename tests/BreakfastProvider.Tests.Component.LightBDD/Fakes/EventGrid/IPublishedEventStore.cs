namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.EventGrid;

public interface IPublishedEventStore
{
    Task<IReadOnlyList<T>> GetPublishedEventsAsync<T>() where T : class;
}
