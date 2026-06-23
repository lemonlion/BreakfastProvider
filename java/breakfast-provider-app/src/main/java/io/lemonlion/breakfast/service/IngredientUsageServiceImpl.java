package io.lemonlion.breakfast.service;

import com.google.cloud.bigquery.BigQuery;
import com.google.cloud.bigquery.DatasetInfo;
import com.google.cloud.bigquery.Field;
import com.google.cloud.bigquery.FieldValueList;
import com.google.cloud.bigquery.InsertAllRequest;
import com.google.cloud.bigquery.InsertAllResponse;
import com.google.cloud.bigquery.QueryJobConfiguration;
import com.google.cloud.bigquery.QueryParameterValue;
import com.google.cloud.bigquery.Schema;
import com.google.cloud.bigquery.StandardSQLTypeName;
import com.google.cloud.bigquery.StandardTableDefinition;
import com.google.cloud.bigquery.TableId;
import com.google.cloud.bigquery.TableInfo;
import com.google.cloud.bigquery.TableResult;
import io.lemonlion.breakfast.model.request.IngredientUsageRequest;
import io.lemonlion.breakfast.model.response.IngredientUsageResponse;
import io.lemonlion.breakfast.model.response.IngredientUsageSummaryResponse;
import java.math.BigDecimal;
import java.time.Instant;
import java.time.LocalDateTime;
import java.time.ZoneOffset;
import java.time.format.DateTimeFormatter;
import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;
import java.util.UUID;
import org.springframework.stereotype.Service;

/** Twin of C# {@code IngredientUsageService}: insert + aggregate queries against BigQuery. */
@Service
public class IngredientUsageServiceImpl implements IngredientUsageService {

    private static final String DATASET = "breakfast_analytics";
    private static final String TABLE = "ingredient_usage";
    private static final DateTimeFormatter TS = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");

    private final BigQuery bigQuery;
    private volatile boolean tableReady;

    public IngredientUsageServiceImpl(BigQuery bigQuery) {
        this.bigQuery = bigQuery;
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
                        Field.of("usage_id", StandardSQLTypeName.STRING),
                        Field.of("ingredient_name", StandardSQLTypeName.STRING),
                        Field.of("quantity_used", StandardSQLTypeName.FLOAT64),
                        Field.of("unit", StandardSQLTypeName.STRING),
                        Field.of("recipe_name", StandardSQLTypeName.STRING),
                        Field.of("recorded_at", StandardSQLTypeName.STRING));
                bigQuery.create(TableInfo.newBuilder(tableId, StandardTableDefinition.of(schema)).build());
            }
            tableReady = true;
        }
    }

    @Override
    public IngredientUsageResponse record(IngredientUsageRequest request) {
        ensureTable();
        String usageId = UUID.randomUUID().toString();
        Instant recordedAt = Instant.now();
        Map<String, Object> row = new LinkedHashMap<>();
        row.put("usage_id", usageId);
        row.put("ingredient_name", request.ingredientName());
        row.put("quantity_used", request.quantityUsed().doubleValue());
        row.put("unit", request.unit());
        row.put("recipe_name", request.recipeName());
        row.put("recorded_at", TS.format(LocalDateTime.ofInstant(recordedAt, ZoneOffset.UTC)));

        InsertAllResponse response = bigQuery.insertAll(
                InsertAllRequest.newBuilder(TableId.of(DATASET, TABLE)).addRow(usageId, row).build());
        if (response.hasErrors()) {
            throw new IllegalStateException("BigQuery insert failed: " + response.getInsertErrors());
        }

        return new IngredientUsageResponse(usageId, request.ingredientName(), request.quantityUsed(),
                request.unit(), request.recipeName(), recordedAt);
    }

    @Override
    public List<IngredientUsageSummaryResponse> getSummary() {
        ensureTable();
        String sql = "SELECT ingredient_name, SUM(quantity_used) AS total_quantity, unit, COUNT(*) AS record_count "
                + "FROM `" + DATASET + "." + TABLE + "` GROUP BY ingredient_name, unit ORDER BY total_quantity DESC";
        List<IngredientUsageSummaryResponse> result = new ArrayList<>();
        for (FieldValueList row : query(QueryJobConfiguration.newBuilder(sql).build())) {
            result.add(new IngredientUsageSummaryResponse(
                    row.get("ingredient_name").getStringValue(),
                    BigDecimal.valueOf(row.get("total_quantity").getDoubleValue()),
                    row.get("unit").getStringValue(),
                    (int) row.get("record_count").getLongValue()));
        }
        return result;
    }

    @Override
    public List<IngredientUsageResponse> listByIngredient(String ingredientName) {
        ensureTable();
        String sql = "SELECT usage_id, ingredient_name, quantity_used, unit, recipe_name, recorded_at "
                + "FROM `" + DATASET + "." + TABLE + "` WHERE ingredient_name = @ingredientName "
                + "ORDER BY recorded_at DESC";
        QueryJobConfiguration config = QueryJobConfiguration.newBuilder(sql)
                .addNamedParameter("ingredientName", QueryParameterValue.string(ingredientName))
                .build();
        List<IngredientUsageResponse> result = new ArrayList<>();
        for (FieldValueList row : query(config)) {
            result.add(new IngredientUsageResponse(
                    row.get("usage_id").getStringValue(),
                    row.get("ingredient_name").getStringValue(),
                    BigDecimal.valueOf(row.get("quantity_used").getDoubleValue()),
                    row.get("unit").getStringValue(),
                    row.get("recipe_name").getStringValue(),
                    LocalDateTime.parse(row.get("recorded_at").getStringValue(), TS).toInstant(ZoneOffset.UTC)));
        }
        return result;
    }

    private Iterable<FieldValueList> query(QueryJobConfiguration config) {
        try {
            TableResult result = bigQuery.query(config);
            return result.iterateAll();
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("BigQuery query interrupted", e);
        }
    }
}
