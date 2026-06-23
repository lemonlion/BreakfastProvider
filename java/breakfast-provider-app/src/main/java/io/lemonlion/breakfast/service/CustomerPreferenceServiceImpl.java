package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.CustomerPreferenceRequest;
import io.lemonlion.breakfast.model.response.CustomerPreferenceResponse;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.stereotype.Service;

/**
 * Twin of C# {@code CustomerPreferenceService}: upsert/get against Google Spanner (via its JDBC driver).
 * The {@code CustomerPreferences} table is created lazily so only this domain touches Spanner.
 */
@Service
public class CustomerPreferenceServiceImpl implements CustomerPreferenceService {

    private static final String CREATE_TABLE = """
            CREATE TABLE IF NOT EXISTS CustomerPreferences (
              CustomerId STRING(100) NOT NULL,
              CustomerName STRING(200),
              PreferredMilkType STRING(50),
              LikesExtraToppings BOOL,
              FavouriteItem STRING(100),
              UpdatedAt TIMESTAMP,
            ) PRIMARY KEY (CustomerId)""";

    private final JdbcTemplate spanner;
    private volatile boolean tableReady;

    public CustomerPreferenceServiceImpl(JdbcTemplate spannerJdbcTemplate) {
        this.spanner = spannerJdbcTemplate;
    }

    private void ensureTable() {
        if (!tableReady) {
            synchronized (this) {
                if (!tableReady) {
                    spanner.execute(CREATE_TABLE);
                    tableReady = true;
                }
            }
        }
    }

    @Override
    public CustomerPreferenceResponse upsert(CustomerPreferenceRequest request) {
        ensureTable();
        Instant now = Instant.now();
        String milk = request.preferredMilkType() == null ? "standard" : request.preferredMilkType();
        String favourite = request.favouriteItem() == null ? "" : request.favouriteItem();
        Timestamp ts = Timestamp.from(now);

        int updated = spanner.update(
                "UPDATE CustomerPreferences SET CustomerName = ?, PreferredMilkType = ?, LikesExtraToppings = ?, "
                        + "FavouriteItem = ?, UpdatedAt = ? WHERE CustomerId = ?",
                request.customerName(), milk, request.likesExtraToppings(), favourite, ts, request.customerId());
        if (updated == 0) {
            spanner.update(
                    "INSERT INTO CustomerPreferences (CustomerId, CustomerName, PreferredMilkType, "
                            + "LikesExtraToppings, FavouriteItem, UpdatedAt) VALUES (?, ?, ?, ?, ?, ?)",
                    request.customerId(), request.customerName(), milk, request.likesExtraToppings(), favourite, ts);
        }

        return new CustomerPreferenceResponse(request.customerId(), request.customerName(), milk,
                request.likesExtraToppings(), favourite, now);
    }

    @Override
    public Optional<CustomerPreferenceResponse> getById(String customerId) {
        ensureTable();
        List<CustomerPreferenceResponse> rows = spanner.query(
                "SELECT CustomerId, CustomerName, PreferredMilkType, LikesExtraToppings, FavouriteItem, UpdatedAt "
                        + "FROM CustomerPreferences WHERE CustomerId = ?",
                (rs, n) -> new CustomerPreferenceResponse(
                        rs.getString("CustomerId"),
                        rs.getString("CustomerName"),
                        rs.getString("PreferredMilkType"),
                        rs.getBoolean("LikesExtraToppings"),
                        rs.getString("FavouriteItem"),
                        rs.getTimestamp("UpdatedAt").toInstant()),
                customerId);
        return rows.stream().findFirst();
    }
}
