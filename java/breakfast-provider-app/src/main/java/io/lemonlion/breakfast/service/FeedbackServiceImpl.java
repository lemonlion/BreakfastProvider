package io.lemonlion.breakfast.service;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import java.sql.Timestamp;
import java.time.Instant;
import java.util.List;
import java.util.Optional;
import java.util.UUID;
import org.springframework.jdbc.core.JdbcTemplate;
import org.springframework.jdbc.core.RowMapper;
import org.springframework.stereotype.Service;

/** Twin of C# {@code FeedbackService}: create/get/list against Google Spanner (via JDBC). */
@Service
public class FeedbackServiceImpl implements FeedbackService {

    private static final String CREATE_TABLE = """
            CREATE TABLE IF NOT EXISTS Feedback (
              FeedbackId STRING(36) NOT NULL,
              CustomerName STRING(200),
              OrderId STRING(100),
              Rating INT64,
              Comment STRING(1000),
              CreatedAt TIMESTAMP,
            ) PRIMARY KEY (FeedbackId)""";

    private static final RowMapper<FeedbackResponse> MAPPER = (rs, n) -> new FeedbackResponse(
            rs.getString("FeedbackId"),
            rs.getString("CustomerName"),
            rs.getString("OrderId"),
            rs.getInt("Rating"),
            rs.getString("Comment"),
            rs.getTimestamp("CreatedAt").toInstant());

    private final JdbcTemplate spanner;
    private volatile boolean tableReady;

    public FeedbackServiceImpl(JdbcTemplate spannerJdbcTemplate) {
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
    public FeedbackResponse create(FeedbackRequest request) {
        ensureTable();
        String feedbackId = UUID.randomUUID().toString();
        Instant now = Instant.now();
        String comment = request.comment() == null ? "" : request.comment();
        spanner.update(
                "INSERT INTO Feedback (FeedbackId, CustomerName, OrderId, Rating, Comment, CreatedAt) "
                        + "VALUES (?, ?, ?, ?, ?, ?)",
                feedbackId, request.customerName(), request.orderId(), (long) request.rating(), comment,
                Timestamp.from(now));
        return new FeedbackResponse(feedbackId, request.customerName(), request.orderId(), request.rating(),
                comment, now);
    }

    @Override
    public Optional<FeedbackResponse> getById(String feedbackId) {
        ensureTable();
        return spanner.query(
                "SELECT FeedbackId, CustomerName, OrderId, Rating, Comment, CreatedAt FROM Feedback "
                        + "WHERE FeedbackId = ?", MAPPER, feedbackId).stream().findFirst();
    }

    @Override
    public List<FeedbackResponse> listByOrder(String orderId) {
        ensureTable();
        return spanner.query(
                "SELECT FeedbackId, CustomerName, OrderId, Rating, Comment, CreatedAt FROM Feedback "
                        + "WHERE OrderId = ? ORDER BY CreatedAt", MAPPER, orderId);
    }
}
