package io.lemonlion.breakfast.testsupport;

import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.KeyStore;
import java.time.Duration;
import org.testcontainers.containers.CosmosDBEmulatorContainer;
import org.testcontainers.containers.KafkaContainer;
import org.testcontainers.utility.DockerImageName;

/**
 * Singleton Testcontainers backends shared across the whole component-test run (started once, reused by
 * every test class and framework). Real backends so Kronikol4J's SDK interceptors capture genuine
 * interactions for the report diagrams. Twin of the C# in-memory-emulator wiring, but Docker-backed.
 */
public final class BreakfastBackends {

    private static final CosmosDBEmulatorContainer COSMOS = new CosmosDBEmulatorContainer(
            DockerImageName.parse("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest"))
            .withStartupTimeout(Duration.ofMinutes(5));

    private static final KafkaContainer KAFKA = new KafkaContainer(
            DockerImageName.parse("confluentinc/cp-kafka:7.6.1"));

    private static final FakeKitchen KITCHEN = new FakeKitchen();

    private static volatile boolean started;

    private BreakfastBackends() {
    }

    public static synchronized void start() {
        if (started) {
            return;
        }
        COSMOS.start();
        configureCosmosTrustStore();
        KAFKA.start();
        KITCHEN.start();
        started = true;
    }

    /** The emulator serves over TLS with a self-signed cert; trust it via the default JVM trust store. */
    private static void configureCosmosTrustStore() {
        try {
            Path keyStoreFile = Files.createTempFile("cosmos-emulator", ".keystore");
            KeyStore keyStore = COSMOS.buildNewKeyStore();
            try (OutputStream out = Files.newOutputStream(keyStoreFile)) {
                keyStore.store(out, COSMOS.getEmulatorKey().toCharArray());
            }
            System.setProperty("javax.net.ssl.trustStore", keyStoreFile.toString());
            System.setProperty("javax.net.ssl.trustStorePassword", COSMOS.getEmulatorKey());
            System.setProperty("javax.net.ssl.trustStoreType", "PKCS12");
        } catch (Exception e) {
            throw new IllegalStateException("Failed to configure Cosmos emulator trust store", e);
        }
    }

    public static String cosmosEndpoint() {
        return COSMOS.getEmulatorEndpoint();
    }

    public static String cosmosKey() {
        return COSMOS.getEmulatorKey();
    }

    public static String kafkaBootstrapServers() {
        return KAFKA.getBootstrapServers();
    }

    public static String kitchenUrl() {
        return KITCHEN.url();
    }

    public static FakeKitchen kitchen() {
        return KITCHEN;
    }
}
