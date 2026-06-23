package io.lemonlion.breakfast.testsupport;

import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.KeyStore;
import java.time.Duration;
import org.testcontainers.containers.CosmosDBEmulatorContainer;
import org.testcontainers.containers.KafkaContainer;
import org.testcontainers.containers.MSSQLServerContainer;
import org.testcontainers.containers.MongoDBContainer;
import org.testcontainers.containers.SpannerEmulatorContainer;
import org.testcontainers.utility.DockerImageName;

/**
 * Singleton Testcontainers backends shared across the whole component-test run (started once, reused by
 * every test class and framework). Real backends so Kronikol4J's SDK interceptors capture genuine
 * interactions for the report diagrams. Twin of the C# in-memory-emulator wiring, but Docker-backed.
 */
public final class BreakfastBackends {

    private static final CosmosDBEmulatorContainer COSMOS = new CosmosDBEmulatorContainer(
            DockerImageName.parse("mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:latest"))
            // The emulator defaults to ~25 partitions, which makes it heavy and slow to become fully
            // responsive (DDL can time out). A small count is ample for the test data and far faster.
            .withEnv("AZURE_COSMOS_EMULATOR_PARTITION_COUNT", "5")
            .withStartupTimeout(Duration.ofMinutes(5));

    private static final KafkaContainer KAFKA = new KafkaContainer(
            DockerImageName.parse("confluentinc/cp-kafka:7.6.1"));

    private static final MSSQLServerContainer<?> SQL_SERVER = new MSSQLServerContainer<>(
            DockerImageName.parse("mcr.microsoft.com/mssql/server:2022-latest"))
            .acceptLicense();

    private static final MongoDBContainer MONGO = new MongoDBContainer(DockerImageName.parse("mongo:7"));

    private static final SpannerEmulatorContainer SPANNER = new SpannerEmulatorContainer(
            DockerImageName.parse("gcr.io/cloud-spanner-emulator/emulator:1.5.23"));

    private static final FakeKitchen KITCHEN = new FakeKitchen();
    private static final FakeSupplier SUPPLIER = new FakeSupplier();

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
        SQL_SERVER.start();
        MONGO.start();
        SPANNER.start();
        KITCHEN.start();
        SUPPLIER.start();
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

    public static String sqlServerJdbcUrl() {
        // encrypt=false keeps the mssql-jdbc 12+ default TLS off for the throwaway test container.
        return SQL_SERVER.getJdbcUrl() + ";encrypt=false";
    }

    public static String sqlServerUsername() {
        return SQL_SERVER.getUsername();
    }

    public static String sqlServerPassword() {
        return SQL_SERVER.getPassword();
    }

    public static String mongoConnectionString() {
        return MONGO.getConnectionString();
    }

    /**
     * Spanner JDBC URL pointed at the emulator. {@code autoConfigEmulator=true} makes the driver use
     * plaintext and auto-create the instance + database, so no admin bootstrap is needed.
     */
    public static String spannerJdbcUrl() {
        return "jdbc:cloudspanner://" + SPANNER.getEmulatorGrpcEndpoint()
                + "/projects/test-project/instances/test-instance/databases/breakfast?autoConfigEmulator=true";
    }

    public static String kitchenUrl() {
        return KITCHEN.url();
    }

    public static FakeKitchen kitchen() {
        return KITCHEN;
    }

    public static String supplierUrl() {
        return SUPPLIER.url();
    }

    public static FakeSupplier supplier() {
        return SUPPLIER;
    }

    /** Resets all in-JVM fakes to their default behaviour (called from each test's setup). */
    public static void resetFakes() {
        KITCHEN.reset();
        SUPPLIER.reset();
    }
}
