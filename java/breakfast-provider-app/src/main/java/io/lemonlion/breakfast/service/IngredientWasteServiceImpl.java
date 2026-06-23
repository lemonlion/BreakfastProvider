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
import io.lemonlion.breakfast.model.request.IngredientWasteRequest;
import io.lemonlion.breakfast.model.response.IngredientWasteResponse;
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
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.stereotype.Service;

/** Twin of C# {@code IngredientWasteService}: insert/query/delete against BigQuery. */
@Service
public class IngredientWasteServiceImpl implements IngredientWasteService {

    private static final String DATASET = "breakfast_analytics";
    private static final String TABLE = "ingredient_waste";
    private static final DateTimeFormatter TS = DateTimeFormatter.ofPattern("yyyy-MM-dd HH:mm:ss");
    private static final Logger log = LoggerFactory.getLogger(IngredientWasteServiceImpl.class);

    private final BigQuery bigQuery;
    private volatile boolean tableReady;

    public IngredientWasteServiceImpl(BigQuery bigQuery) {
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
                        Field.of("waste_id", StandardSQLTypeName.STRING),
                        Field.of("ingredient_name", StandardSQLTypeName.STRING),
                        Field.of("quantity_wasted", StandardSQLTypeName.FLOAT64),
                        Field.of("unit", StandardSQLTypeName.STRING),
                        Field.of("recipe_name", StandardSQLTypeName.STRING),
                        Field.of("reason", StandardSQLTypeName.STRING),
                        Field.of("recorded_at", StandardSQLTypeName.STRING));
                bigQuery.create(TableInfo.newBuilder(tableId, StandardTableDefinition.of(schema)).build());
            }
            tableReady = true;
        }
    }

    @Override
    public IngredientWasteResponse record(IngredientWasteRequest request) {
        ensureTable();
        String wasteId = UUID.randomUUID().toString();
        Instant recordedAt = Instant.now();
        Map<String, Object> row = new LinkedHashMap<>();
        row.put("waste_id", wasteId);
        row.put("ingredient_name", request.ingredientName());
        row.put("quantity_wasted", request.quantityWasted().doubleValue());
        row.put("unit", request.unit());
        row.put("recipe_name", request.recipeName());
        row.put("reason", request.reason());
        row.put("recorded_at", TS.format(LocalDateTime.ofInstant(recordedAt, ZoneOffset.UTC)));

        InsertAllResponse response = bigQuery.insertAll(
                InsertAllRequest.newBuilder(TableId.of(DATASET, TABLE)).addRow(wasteId, row).build());
        if (response.hasErrors()) {
            throw new IllegalStateException("BigQuery insert failed: " + response.getInsertErrors());
        }

        return new IngredientWasteResponse(wasteId, request.ingredientName(), request.quantityWasted(),
                request.unit(), request.recipeName(), request.reason(), recordedAt);
    }

    @Override
    public List<IngredientWasteResponse> listByRecipe(String recipeName) {
        ensureTable();
        String sql = "SELECT waste_id, ingredient_name, quantity_wasted, unit, recipe_name, reason, recorded_at "
                + "FROM `" + DATASET + "." + TABLE + "` WHERE recipe_name = @recipeName ORDER BY recorded_at DESC";
        QueryJobConfiguration config = QueryJobConfiguration.newBuilder(sql)
                .addNamedParameter("recipeName", QueryParameterValue.string(recipeName))
                .build();
        List<IngredientWasteResponse> result = new ArrayList<>();
        try {
            TableResult queryResult = bigQuery.query(config);
            for (FieldValueList row : queryResult.iterateAll()) {
                result.add(new IngredientWasteResponse(
                        row.get("waste_id").getStringValue(),
                        row.get("ingredient_name").getStringValue(),
                        BigDecimal.valueOf(row.get("quantity_wasted").getDoubleValue()),
                        row.get("unit").getStringValue(),
                        row.get("recipe_name").getStringValue(),
                        row.get("reason").getStringValue(),
                        LocalDateTime.parse(row.get("recorded_at").getStringValue(), TS).toInstant(ZoneOffset.UTC)));
            }
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("BigQuery query interrupted", e);
        }
        return result;
    }

    @Override
    public void delete(String wasteId) {
        ensureTable();
        String sql = "DELETE FROM `" + DATASET + "." + TABLE + "` WHERE waste_id = @wasteId";
        QueryJobConfiguration config = QueryJobConfiguration.newBuilder(sql)
                .addNamedParameter("wasteId", QueryParameterValue.string(wasteId))
                .build();
        try {
            bigQuery.query(config);
        } catch (InterruptedException e) {
            Thread.currentThread().interrupt();
            throw new IllegalStateException("BigQuery delete interrupted", e);
        } catch (RuntimeException e) {
            // Mirrors the C# endpoint which always returns 204; tolerate emulator DML-on-stream limits.
            log.warn("BigQuery delete of waste {} failed (tolerated)", wasteId, e);
        }
    }
}
