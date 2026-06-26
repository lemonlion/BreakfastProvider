package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import com.fasterxml.jackson.core.type.TypeReference;
import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.List;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Feedback domain component tests (JUnit 5) — Spanner persistence. */
@DisplayName("Feedback")
class FeedbackComponentTest extends ComponentTestBase {

    @Test
    @DisplayName("feedback is created and retrievable by id")
    void createAndRetrieve() {
        TestResponse created = client.post("/feedback",
                new FeedbackRequest("Alice", "order-" + UUID.randomUUID(), 5, "Loved it"));
        assertThat(created.status()).isEqualTo(201);
        FeedbackResponse feedback = created.as(FeedbackResponse.class);
        assertThat(feedback.feedbackId()).isNotBlank();
        assertThat(feedback.rating()).isEqualTo(5);

        TestResponse fetched = client.get("/feedback/" + feedback.feedbackId());
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(FeedbackResponse.class).customerName()).isEqualTo("Alice");
    }

    @Test
    @DisplayName("submitting feedback returns the created feedback")
    void submitReturnsCreated() {
        String customer = "Customer-" + UUID.randomUUID();
        TestResponse created = client.post("/feedback",
                new FeedbackRequest(customer, "order-" + UUID.randomUUID(), 4, "Great pancakes!"));
        assertThat(created.status()).isEqualTo(201);
        FeedbackResponse feedback = created.as(FeedbackResponse.class);
        assertThat(feedback.customerName()).isEqualTo(customer);
        assertThat(feedback.rating()).isEqualTo(4);
    }

    @Test
    @DisplayName("listing feedback for an order returns the submitted feedback")
    void listByOrder() {
        String orderId = "order-" + UUID.randomUUID();
        FeedbackResponse created = client.post("/feedback",
                new FeedbackRequest("Bob", orderId, 3, "Decent")).as(FeedbackResponse.class);

        TestResponse list = client.get("/feedback/order/" + orderId);
        assertThat(list.status()).isEqualTo(200);
        assertThat(list.as(new TypeReference<List<FeedbackResponse>>() { }))
                .anyMatch(f -> f.feedbackId().equals(created.feedbackId()));
    }

    @Test
    @DisplayName("feedback with a missing customer name is rejected")
    void rejectsMissingCustomerName() {
        TestResponse response = client.post("/feedback",
                new FeedbackRequest(null, "order-" + UUID.randomUUID(), 3, "Missing name"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Customer Name' must not be empty.")).isTrue();
    }

    @Test
    @DisplayName("a rating outside 1-5 is rejected")
    void rejectsBadRating() {
        TestResponse response = client.post("/feedback",
                new FeedbackRequest("Alice", "order-1", 9, "Too high"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Rating' must be between 1 and 5.")).isTrue();
    }

    @Test
    @DisplayName("retrieving unknown feedback returns 404")
    void getMissing() {
        assertThat(client.get("/feedback/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }
}
