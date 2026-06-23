package io.lemonlion.breakfast.events.outbox;

import com.azure.core.credential.AzureKeyCredential;
import com.azure.core.util.BinaryData;
import com.azure.messaging.eventgrid.EventGridEvent;
import com.azure.messaging.eventgrid.EventGridPublisherClient;
import com.azure.messaging.eventgrid.EventGridPublisherClientBuilder;
import io.lemonlion.breakfast.config.EventGridConfig;
import io.lemonlion.breakfast.storage.OutboxMessage;
import org.springframework.stereotype.Component;

/** Twin of C# {@code EventGridOutboxDispatcher}: ships outbox messages to Azure Event Grid. */
@Component
public class EventGridOutboxDispatcher implements OutboxDispatcher {

    private final EventGridConfig config;
    private volatile EventGridPublisherClient<EventGridEvent> client;

    public EventGridOutboxDispatcher(EventGridConfig config) {
        this.config = config;
    }

    @Override
    public String destination() {
        return OutboxDestinations.EVENT_GRID;
    }

    @Override
    public void dispatch(OutboxMessage message) {
        if (!config.isEnabled()) {
            return;
        }
        EventGridEvent event = new EventGridEvent(
                config.getSubject(), message.getEventType(), BinaryData.fromString(message.getPayload()), "1.0");
        client().sendEvent(event);
    }

    private EventGridPublisherClient<EventGridEvent> client() {
        EventGridPublisherClient<EventGridEvent> local = client;
        if (local == null) {
            synchronized (this) {
                local = client;
                if (local == null) {
                    local = new EventGridPublisherClientBuilder()
                            .endpoint(config.getEndpoint())
                            .credential(new AzureKeyCredential(config.getKey()))
                            .buildEventGridEventPublisherClient();
                    client = local;
                }
            }
        }
        return local;
    }
}
