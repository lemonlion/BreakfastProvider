package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.time.Instant;
import java.time.temporal.ChronoUnit;
import org.testng.annotations.Test;

/** Reservations domain component tests (TestNG). */
public class ReservationsTestNgTest extends ComponentTestBaseNg {

    private static ReservationRequest valid() {
        return new ReservationRequest("Alice", 12, 4, Instant.now().plus(1, ChronoUnit.DAYS), "555-0100");
    }

    @Test
    public void createAndRetrieve() {
        TestResponse created = client.post("/reservations", valid());
        assertThat(created.status()).isEqualTo(201);
        ReservationResponse reservation = created.as(ReservationResponse.class);
        assertThat(reservation.id()).isPositive();
        assertThat(reservation.status()).isEqualTo("Confirmed");
        assertThat(client.get("/reservations/" + reservation.id()).status()).isEqualTo(200);
    }

    @Test
    public void getMissing() {
        assertThat(client.get("/reservations/999999").status()).isEqualTo(404);
    }

    @Test
    public void cancelThenConflict() {
        ReservationResponse reservation = client.post("/reservations", valid()).as(ReservationResponse.class);
        assertThat(client.patch("/reservations/" + reservation.id() + "/cancel", "").status()).isEqualTo(200);
        assertThat(client.patch("/reservations/" + reservation.id() + "/cancel", "").status()).isEqualTo(409);
    }

    @Test
    public void rejectsBadPartySize() {
        ReservationRequest bad = new ReservationRequest("Alice", 12, 99, Instant.now().plus(1, ChronoUnit.DAYS), null);
        TestResponse response = client.post("/reservations", bad);
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Party Size' must be between 1 and 20.")).isTrue();
    }

    @Test
    public void deleteReturns204() {
        ReservationResponse reservation = client.post("/reservations", valid()).as(ReservationResponse.class);
        assertThat(client.delete("/reservations/" + reservation.id()).status()).isEqualTo(204);
    }
}
