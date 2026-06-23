package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.FeedbackRequest;
import io.lemonlion.breakfast.model.response.FeedbackResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.UUID;
import org.testng.annotations.Test;

/** Feedback domain component tests (TestNG). */
public class FeedbackTestNgTest extends ComponentTestBaseNg {

    @Test
    public void createAndRetrieve() {
        TestResponse created = client.post("/feedback",
                new FeedbackRequest("Alice", "order-" + UUID.randomUUID(), 5, "Loved it"));
        assertThat(created.status()).isEqualTo(201);
        FeedbackResponse feedback = created.as(FeedbackResponse.class);
        assertThat(feedback.feedbackId()).isNotBlank();
        assertThat(client.get("/feedback/" + feedback.feedbackId()).status()).isEqualTo(200);
    }

    @Test
    public void rejectsBadRating() {
        TestResponse response = client.post("/feedback", new FeedbackRequest("Alice", "order-1", 9, "Too high"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Rating' must be between 1 and 5.")).isTrue();
    }

    @Test
    public void getMissing() {
        assertThat(client.get("/feedback/unknown-" + UUID.randomUUID()).status()).isEqualTo(404);
    }
}
