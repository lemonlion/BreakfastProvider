package io.lemonlion.breakfast.tests.cucumber;

import static org.assertj.core.api.Assertions.assertThat;

import io.cucumber.java.en.Given;
import io.cucumber.java.en.Then;
import io.cucumber.java.en.When;
import io.lemonlion.breakfast.model.request.ReservationRequest;
import io.lemonlion.breakfast.model.response.ReservationResponse;
import java.time.Instant;
import java.time.temporal.ChronoUnit;

/** Cucumber step definitions for the Reservations domain. */
public class ReservationSteps {

    private final ScenarioContext ctx;
    private ReservationResponse reservation;

    public ReservationSteps(ScenarioContext ctx) {
        this.ctx = ctx;
    }

    @When("a reservation for {string} is created")
    public void aReservationIsCreated(String customerName) {
        ReservationRequest request = new ReservationRequest(
                customerName, 12, 4, Instant.now().plus(1, ChronoUnit.DAYS), "555-0100");
        ctx.lastResponse = ctx.client().post("/reservations", request);
        if (ctx.lastResponse.status() == 201) {
            reservation = ctx.lastResponse.as(ReservationResponse.class);
        }
    }

    @Given("a confirmed reservation")
    public void aConfirmedReservation() {
        ReservationRequest request = new ReservationRequest(
                "Alice", 12, 4, Instant.now().plus(1, ChronoUnit.DAYS), "555-0100");
        reservation = ctx.client().post("/reservations", request).as(ReservationResponse.class);
    }

    @When("the reservation is cancelled")
    public void theReservationIsCancelled() {
        ctx.lastResponse = ctx.client().patch("/reservations/" + reservation.id() + "/cancel", "");
    }

    @When("the reservation is cancelled again")
    public void theReservationIsCancelledAgain() {
        ctx.client().patch("/reservations/" + reservation.id() + "/cancel", "");
        ctx.lastResponse = ctx.client().patch("/reservations/" + reservation.id() + "/cancel", "");
    }

    @Then("the reservation is confirmed")
    public void theReservationIsConfirmed() {
        assertThat(ctx.lastResponse.status()).isEqualTo(201);
        assertThat(reservation.status()).isEqualTo("Confirmed");
    }
}
