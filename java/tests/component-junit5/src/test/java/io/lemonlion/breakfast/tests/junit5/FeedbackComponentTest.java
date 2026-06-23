package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
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
