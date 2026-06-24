package io.lemonlion.breakfast.testsupport;

import java.io.OutputStream;
import java.nio.file.Files;
import java.nio.file.Path;
import java.security.KeyStore;
import java.time.Duration;
import org.testcontainers.containers.CosmosDBEmulatorContainer;
import org.testcontainers.containers.KafkaContainer;
import com.google.api.gax.core.NoCredentialsProvider;
import com.google.api.gax.grpc.GrpcTransportChannel;
import com.google.api.gax.rpc.FixedTransportChannelProvider;
import com.google.api.gax.rpc.TransportChannelProvider;
import com.google.cloud.pubsub.v1.Publisher;
import com.google.cloud.pubsub.v1.SubscriptionAdminClient;
import com.google.cloud.pubsub.v1.SubscriptionAdminSettings;
import com.google.cloud.pubsub.v1.TopicAdminClient;
import com.google.cloud.pubsub.v1.TopicAdminSettings;
import com.google.protobuf.ByteString;
import com.google.pubsub.v1.PubsubMessage;
import com.google.pubsub.v1.PushConfig;
import com.google.pubsub.v1.SubscriptionName;
import com.google.pubsub.v1.TopicName;
import io.grpc.ManagedChannel;
import io.grpc.ManagedChannelBuilder;
import org.testcontainers.containers.GenericContainer;
import org.testcontainers.containers.MSSQLServerContainer;
import org.testcontainers.containers.MongoDBContainer;
import org.testcontainers.containers.PubSubEmulatorContainer;
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

    private static final String BIGQUERY_PROJECT = "test-project";
    private static final int BIGQUERY_PORT = 9050;
    @SuppressWarnings("resource")
    private static final GenericContainer<?> BIGQUERY = new GenericContainer<>(
            DockerImageName.parse("ghcr.io/goccy/bigquery-emulator:0.6.6"))
            .withExposedPorts(BIGQUERY_PORT)
            .withCommand("--project=" + BIGQUERY_PROJECT, "--dataset=breakfast_analytics");

    private static final String PUBSUB_PROJECT = "test-project";
    private static final String FEEDBACK_TOPIC = "customer-feedback";
    private static final String FEEDBACK_SUBSCRIPTION = "customer-feedback-sub";
    private static final String BATCH_TOPIC = "batch-completions";
    private static final String BATCH_SUBSCRIPTION = "batch-completions-sub";
    private static final PubSubEmulatorContainer PUBSUB = new PubSubEmulatorContainer(
            DockerImageName.parse("gcr.io/google.com/cloudsdktool/google-cloud-cli:emulators"));
    private static Publisher feedbackPublisher;

    private static final FakeKitchen KITCHEN = new FakeKitchen();
    private static final FakeSupplier SUPPLIER = new FakeSupplier();
    private static final FakeMilkService COW = new FakeMilkService("/milk", "{\"milk\":\"Some_Milk\"}");
    private static final FakeMilkService GOAT =
            new FakeMilkService("/goat-milk", "{\"goatMilk\":\"Some_Fresh_Goat_Milk\"}");

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
        BIGQUERY.start();
        PUBSUB.start();
        configurePubSub();
        KITCHEN.start();
        SUPPLIER.start();
        COW.start();
        GOAT.start();
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

    public static String bigQueryEndpoint() {
        return "http://" + BIGQUERY.getHost() + ":" + BIGQUERY.getMappedPort(BIGQUERY_PORT);
    }

    public static String bigQueryProjectId() {
        return BIGQUERY_PROJECT;
    }

    public static String pubSubEndpoint() {
        return PUBSUB.getEmulatorEndpoint();
    }

    public static String pubSubProjectId() {
        return PUBSUB_PROJECT;
    }

    public static String feedbackSubscription() {
        return FEEDBACK_SUBSCRIPTION;
    }

    /** Serializes the event and publishes it to the customer-feedback Pub/Sub topic. */
    public static void publishCustomerFeedback(Object event) {
        try {
            publishCustomerFeedback(JsonMappers.instance().writeValueAsString(event));
        } catch (Exception e) {
            throw new IllegalStateException("Failed to serialize customer feedback event", e);
        }
    }

    /** Publishes a customer-feedback event JSON to the Pub/Sub topic the SUT consumer subscribes to. */
    public static void publishCustomerFeedback(String json) {
        try {
            feedbackPublisher.publish(PubsubMessage.newBuilder()
                    .setData(ByteString.copyFromUtf8(json)).build()).get();
        } catch (Exception e) {
            throw new IllegalStateException("Failed to publish customer feedback", e);
        }
    }

    /** Creates the customer-feedback topic + subscription on the emulator and a publisher for tests. */
    private static void configurePubSub() {
        try {
            ManagedChannel channel = ManagedChannelBuilder.forTarget(PUBSUB.getEmulatorEndpoint())
                    .usePlaintext().build();
            TransportChannelProvider channelProvider =
                    FixedTransportChannelProvider.create(GrpcTransportChannel.create(channel));
            NoCredentialsProvider creds = NoCredentialsProvider.create();
            TopicName topicName = TopicName.of(PUBSUB_PROJECT, FEEDBACK_TOPIC);
            TopicName batchTopicName = TopicName.of(PUBSUB_PROJECT, BATCH_TOPIC);
            try (TopicAdminClient topicAdmin = TopicAdminClient.create(TopicAdminSettings.newBuilder()
                    .setTransportChannelProvider(channelProvider).setCredentialsProvider(creds).build())) {
                topicAdmin.createTopic(topicName);
                topicAdmin.createTopic(batchTopicName);
            }
            try (SubscriptionAdminClient subAdmin = SubscriptionAdminClient.create(SubscriptionAdminSettings.newBuilder()
                    .setTransportChannelProvider(channelProvider).setCredentialsProvider(creds).build())) {
                subAdmin.createSubscription(SubscriptionName.of(PUBSUB_PROJECT, FEEDBACK_SUBSCRIPTION),
                        topicName, PushConfig.getDefaultInstance(), 10);
                subAdmin.createSubscription(SubscriptionName.of(PUBSUB_PROJECT, BATCH_SUBSCRIPTION),
                        batchTopicName, PushConfig.getDefaultInstance(), 10);
            }
            feedbackPublisher = Publisher.newBuilder(topicName)
                    .setChannelProvider(channelProvider).setCredentialsProvider(creds).build();
        } catch (Exception e) {
            throw new IllegalStateException("Failed to configure Pub/Sub emulator", e);
        }
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

    public static String cowUrl() {
        return COW.url();
    }

    public static FakeMilkService cow() {
        return COW;
    }

    public static String goatUrl() {
        return GOAT.url();
    }

    public static FakeMilkService goat() {
        return GOAT;
    }

    /** Resets all in-JVM fakes to their default behaviour (called from each test's setup). */
    public static void resetFakes() {
        KITCHEN.reset();
        SUPPLIER.reset();
        COW.reset();
        GOAT.reset();
    }
}
