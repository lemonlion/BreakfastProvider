package io.lemonlion.breakfast.events;

/** Twin of C# {@code PubSubEventPublisher<T>}: publishes domain events to Google Cloud Pub/Sub. */
public interface PubSubPublisher {

    void publish(Object event);
}
