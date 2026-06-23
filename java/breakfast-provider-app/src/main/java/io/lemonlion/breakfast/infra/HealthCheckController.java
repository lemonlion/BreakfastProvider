package io.lemonlion.breakfast.infra;

import com.azure.cosmos.CosmosClient;
import io.lemonlion.breakfast.config.DownstreamConfig;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.Properties;
import java.util.concurrent.TimeUnit;
import org.apache.kafka.clients.admin.AdminClient;
import org.apache.kafka.clients.admin.AdminClientConfig;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.http.ResponseEntity;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.RestController;
import org.springframework.web.client.RestTemplate;

/**
 * Twin of the C# health-check pipeline ({@code AddDownstreamServiceChecks}/{@code AddInfrastructureChecks}
 * + {@code HealthCheckResponseWriter}). Reports each named dependency under {@code results} with a
 * {@code status}/{@code description}/{@code data} shape and an overall worst-case {@code status}.
 * Healthy/Degraded return 200; Unhealthy returns 503 (matching ASP.NET's default health endpoint).
 */
@RestController
public class HealthCheckController {

    /** Check names — mirror C# {@code HealthCheckNames}. */
    static final String COW = "CowService";
    static final String GOAT = "GoatService";
    static final String SUPPLIER = "SupplierService";
    static final String KITCHEN = "KitchenService";
    static final String COSMOS = "CosmosDb";
    static final String KAFKA = "Kafka";

    private static final String HEALTHY = "Healthy";
    private static final String DEGRADED = "Degraded";
    private static final String UNHEALTHY = "Unhealthy";

    private final RestTemplate restTemplate;
    private final DownstreamConfig downstream;
    private final CosmosClient cosmosClient;
    private final String kafkaBootstrap;

    public HealthCheckController(RestTemplateBuilder builder, DownstreamConfig downstream,
                                 CosmosClient cosmosClient,
                                 @Value("${spring.kafka.bootstrap-servers:}") String kafkaBootstrap) {
        this.restTemplate = builder.build();
        this.downstream = downstream;
        this.cosmosClient = cosmosClient;
        this.kafkaBootstrap = kafkaBootstrap;
    }

    @GetMapping("/health")
    public ResponseEntity<Map<String, Object>> health() {
        Map<String, Object> results = new LinkedHashMap<>();
        results.put(COW, downstreamCheck(COW, downstream.getCowServiceUrl()));
        results.put(GOAT, downstreamCheck(GOAT, downstream.getGoatServiceUrl()));
        results.put(SUPPLIER, downstreamCheck(SUPPLIER, downstream.getSupplierServiceUrl()));
        results.put(KITCHEN, downstreamCheck(KITCHEN, downstream.getKitchenServiceUrl()));
        results.put(COSMOS, cosmosCheck());
        results.put(KAFKA, kafkaCheck());

        String overall = overall(results);
        Map<String, Object> body = new LinkedHashMap<>();
        body.put("status", overall);
        body.put("results", results);
        return ResponseEntity.status(UNHEALTHY.equals(overall) ? 503 : 200).body(body);
    }

    /** Downstream HTTP service: a non-2xx or error degrades (never fully unhealthy), mirroring C#. */
    private Map<String, Object> downstreamCheck(String name, String baseUrl) {
        try {
            ResponseEntity<String> response = restTemplate.getForEntity(baseUrl + "/health", String.class);
            if (response.getStatusCode().is2xxSuccessful()) {
                return entry(HEALTHY, name + " is reachable.");
            }
            return entry(DEGRADED, name + " returned status code " + response.getStatusCode().value() + ".");
        } catch (RuntimeException ex) {
            return entry(DEGRADED, name + " is unreachable.");
        }
    }

    private Map<String, Object> cosmosCheck() {
        try {
            cosmosClient.readAllDatabases().iterator().hasNext();
            return entry(HEALTHY, "Cosmos DB is reachable.");
        } catch (RuntimeException ex) {
            return entry(UNHEALTHY, "Cosmos DB is unreachable.");
        }
    }

    private Map<String, Object> kafkaCheck() {
        if (kafkaBootstrap == null || kafkaBootstrap.isBlank()) {
            return entry(UNHEALTHY, "Kafka is not configured.");
        }
        Properties props = new Properties();
        props.put(AdminClientConfig.BOOTSTRAP_SERVERS_CONFIG, kafkaBootstrap);
        props.put(AdminClientConfig.REQUEST_TIMEOUT_MS_CONFIG, 5000);
        try (AdminClient admin = AdminClient.create(props)) {
            admin.describeCluster().clusterId().get(5, TimeUnit.SECONDS);
            return entry(HEALTHY, "Kafka is reachable.");
        } catch (Exception ex) {
            return entry(UNHEALTHY, "Kafka is unreachable.");
        }
    }

    private static Map<String, Object> entry(String status, String description) {
        Map<String, Object> entry = new LinkedHashMap<>();
        entry.put("status", status);
        entry.put("description", description);
        entry.put("data", new LinkedHashMap<>());
        return entry;
    }

    @SuppressWarnings("unchecked")
    private static String overall(Map<String, Object> results) {
        String worst = HEALTHY;
        for (Object value : results.values()) {
            String status = (String) ((Map<String, Object>) value).get("status");
            if (UNHEALTHY.equals(status)) {
                return UNHEALTHY;
            }
            if (DEGRADED.equals(status)) {
                worst = DEGRADED;
            }
        }
        return worst;
    }
}
