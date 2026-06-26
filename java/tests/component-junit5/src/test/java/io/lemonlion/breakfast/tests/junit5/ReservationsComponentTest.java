package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** Reservations domain component tests (JUnit 5) — relational persistence. */
@DisplayName("Reservations")
class ReservationsComponentTest extends ComponentTestBase {

    private static ReservationRequest valid() {
        return new ReservationRequest("Alice", 12, 4, Instant.now().plus(1, ChronoUnit.DAYS), "555-0100");
    }

    @Test
    @DisplayName("a reservation is created, confirmed, and retrievable")
    void createAndRetrieve() {
        TestResponse created = client.post("/reservations", valid());
        assertThat(created.status()).isEqualTo(201);
        ReservationResponse reservation = created.as(ReservationResponse.class);
        assertThat(reservation.id()).isPositive();
        assertThat(reservation.status()).isEqualTo("Confirmed");

        TestResponse fetched = client.get("/reservations/" + reservation.id());
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(ReservationResponse.class).customerName()).isEqualTo("Alice");
    }

    @Test
    @DisplayName("retrieving a non-existent reservation returns 404")
    void getMissing() {
        assertThat(client.get("/reservations/999999").status()).isEqualTo(404);
    }

    @Test
    @DisplayName("cancelling twice returns a 409 conflict")
    void cancelThenConflict() {
        ReservationResponse reservation = client.post("/reservations", valid()).as(ReservationResponse.class);

        assertThat(client.patch("/reservations/" + reservation.id() + "/cancel", "").status()).isEqualTo(200);
        assertThat(client.patch("/reservations/" + reservation.id() + "/cancel", "").status()).isEqualTo(409);
    }

    @Test
    @DisplayName("a party size outside 1-20 is rejected")
    void rejectsBadPartySize() {
        ReservationRequest bad = new ReservationRequest("Alice", 12, 99, Instant.now().plus(1, ChronoUnit.DAYS), null);
        TestResponse response = client.post("/reservations", bad);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Party Size' must be between 1 and 20.")).isTrue();
    }

    @Test
    @DisplayName("deleting a reservation returns 204")
    void deleteReturns204() {
        ReservationResponse reservation = client.post("/reservations", valid()).as(ReservationResponse.class);
        assertThat(client.delete("/reservations/" + reservation.id()).status()).isEqualTo(204);
    }
}
