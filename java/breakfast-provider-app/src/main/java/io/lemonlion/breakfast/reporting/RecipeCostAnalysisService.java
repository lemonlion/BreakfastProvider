package io.lemonlion.breakfast.reporting;

import com.google.cloud.bigquery.BigQuery;
import com.google.cloud.bigquery.DatasetInfo;
import com.google.cloud.bigquery.Field;
import com.google.cloud.bigquery.InsertAllRequest;
import com.google.cloud.bigquery.InsertAllResponse;
import com.google.cloud.bigquery.Schema;
import com.google.cloud.bigquery.StandardSQLTypeName;
import com.google.cloud.bigquery.StandardTableDefinition;
import com.google.cloud.bigquery.TableId;
import com.google.cloud.bigquery.TableInfo;
import io.lemonlion.breakfast.config.DownstreamConfig;
import io.lemonlion.breakfast.model.event.RecipeCostCalculatedEvent;
import io.lemonlion.breakfast.notification.NotificationClient;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.LinkedHashMap;
import java.util.Map;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.boot.web.client.RestTemplateBuilder;
import org.springframework.stereotype.Service;
import org.springframework.web.client.RestTemplate;

/**
 * Twin of C# {@code RecipeCostAnalysisService}: on a consumed recipe-cost event, store it in BigQuery
 * ({@code recipe_costs}), send a gRPC notification, and notify the Kitchen service.
 */
@Service
public class RecipeCostAnalysisService {

    private static final String DATASET = "breakfast_analytics";
    private static final String TABLE = "recipe_costs";
    private static final DateTimeFormatter TS = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");
    private static final Logger log = LoggerFactory.getLogger(RecipeCostAnalysisService.class);

    private final BigQuery bigQuery;
    private final NotificationClient notificationClient;
    private final RestTemplate restTemplate;
    private final String kitchenUrl;
    private volatile boolean tableReady;

    public RecipeCostAnalysisService(BigQuery bigQuery, NotificationClient notificationClient,
                                     RestTemplateBuilder builder, DownstreamConfig downstreamConfig) {
        this.bigQuery = bigQuery;
        this.notificationClient = notificationClient;
        this.restTemplate = builder.build();
        this.kitchenUrl = downstreamConfig.getKitchenServiceUrl();
    }

    private void ensureTable() {
        if (tableReady) {
            return;
        }
        synchronized (this) {
            if (tableReady) {
                return;
            }
            if (bigQuery.getDataset(DATASET) == null) {
                bigQuery.create(DatasetInfo.newBuilder(DATASET).build());
            }
            TableId tableId = TableId.of(DATASET, TABLE);
            if (bigQuery.getTable(tableId) == null) {
                Schema schema = Schema.of(
                        Field.of("calculation_id", StandardSQLTypeName.STRING),
                        Field.of("recipe_name", StandardSQLTypeName.STRING),
                        Field.of("ingredients", StandardSQLTypeName.STRING),
                        Field.of("total_cost", StandardSQLTypeName.FLOAT64),
                        Field.of("currency", StandardSQLTypeName.STRING),
                        Field.of("calculated_at", StandardSQLTypeName.STRING));
                bigQuery.create(TableInfo.newBuilder(tableId, StandardTableDefinition.of(schema)).build());
            }
            tableReady = true;
        }
    }

    public void processCostCalculation(RecipeCostCalculatedEvent event) {
        ensureTable();
        Map<String, Object> row = new LinkedHashMap<>();
        row.put("calculation_id", event.calculationId().toString());
        row.put("recipe_name", event.recipeName());
        row.put("ingredients", String.join(",", event.ingredients()));
        row.put("total_cost", event.totalCost().doubleValue());
        row.put("currency", event.currency());
        row.put("calculated_at", TS.format(LocalDateTime.ofInstant(event.calculatedAt(), ZoneOffset.UTC)));
        InsertAllResponse response = bigQuery.insertAll(
                InsertAllRequest.newBuilder(TableId.of(DATASET, TABLE))
                        .addRow(event.calculationId().toString(), row).build());
        if (response.hasErrors()) {
            throw new IllegalStateException("BigQuery insert failed: " + response.getInsertErrors());
        }

        notificationClient.sendOrderConfirmation(
                event.calculationId(), event.recipeName(), event.ingredients().size());

        Map<String, Object> body = new LinkedHashMap<>();
        body.put("recipeName", event.recipeName());
        body.put("totalCost", event.totalCost());
        body.put("currency", event.currency());
        body.put("ingredients", event.ingredients());
        restTemplate.postForEntity(kitchenUrl + "/prepare", body, Void.class);

        log.info("Processed recipe cost {} for recipe {}", event.calculationId(), event.recipeName());
    }
}
