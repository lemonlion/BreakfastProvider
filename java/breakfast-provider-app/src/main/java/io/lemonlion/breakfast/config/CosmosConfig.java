package io.lemonlion.breakfast.config;

import org.springframework.boot.context.properties.ConfigurationProperties;

/** Twin of C# {@code CosmosConfig}: connection + database settings for Azure Cosmos DB. */
@ConfigurationProperties(prefix = "cosmos")
public class CosmosConfig {

    private String endpoint = "https://localhost:8081";
    /** Default is the well-known Cosmos emulator key. */
    private String key = "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
    private String databaseName = "breakfast";
    /**
     * Whether the SDK performs endpoint discovery (multi-region routing). Must be {@code false} against
     * the Cosmos emulator under Testcontainers, where the discovered replica address is the container's
     * unreachable internal IP.
     */
    private boolean endpointDiscoveryEnabled = true;

    public String getEndpoint() {
        return endpoint;
    }

    public void setEndpoint(String endpoint) {
        this.endpoint = endpoint;
    }

    public String getKey() {
        return key;
    }

    public void setKey(String key) {
        this.key = key;
    }

    public String getDatabaseName() {
        return databaseName;
    }

    public void setDatabaseName(String databaseName) {
        this.databaseName = databaseName;
    }

    public boolean isEndpointDiscoveryEnabled() {
        return endpointDiscoveryEnabled;
    }

    public void setEndpointDiscoveryEnabled(boolean endpointDiscoveryEnabled) {
        this.endpointDiscoveryEnabled = endpointDiscoveryEnabled;
    }
}
