package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/**
 * Azure Event Hubs settings (twin of the C# {@code EventHubConfig}). Empty connection string => disabled
 * (the publisher logs, the consumer does not start). Against the emulator the connection string carries
 * {@code UseDevelopmentEmulator=true}; the consumer checkpoints to the Azurite blob store.
 */
@ConfigurationProperties(prefix = "event-hub")
public class EventHubConfig {

    private String connectionString = "";
    private String eventHubName = "breakfast-equipment-alerts";
    private String consumerGroup = "$Default";
    private String blobConnectionString = "";
    private String checkpointContainer = "equipment-alerts-checkpoints";

    public boolean isEnabled() {
        return !connectionString.isBlank();
    }

    public String getConnectionString() {
        return connectionString;
    }

    public void setConnectionString(String connectionString) {
        this.connectionString = connectionString;
    }

    public String getEventHubName() {
        return eventHubName;
    }

    public void setEventHubName(String eventHubName) {
        this.eventHubName = eventHubName;
    }

    public String getConsumerGroup() {
        return consumerGroup;
    }

    public void setConsumerGroup(String consumerGroup) {
        this.consumerGroup = consumerGroup;
    }

    public String getBlobConnectionString() {
        return blobConnectionString;
    }

    public void setBlobConnectionString(String blobConnectionString) {
        this.blobConnectionString = blobConnectionString;
    }

    public String getCheckpointContainer() {
        return checkpointContainer;
    }

    public void setCheckpointContainer(String checkpointContainer) {
        this.checkpointContainer = checkpointContainer;
    }
}
