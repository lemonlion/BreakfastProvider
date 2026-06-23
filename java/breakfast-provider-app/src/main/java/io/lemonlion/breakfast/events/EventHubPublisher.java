package io.lemonlion.breakfast.events;

/** Twin of C# {@code EventHubEventPublisher<T>}: publishes domain events to Azure Event Hubs. */
public interface EventHubPublisher {

    void publish(Object event);
}
