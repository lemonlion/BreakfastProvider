package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.ReservationRequest
import io.lemonlion.breakfast.model.response.ReservationResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

import java.time.Instant
import java.time.temporal.ChronoUnit

/** Reservations domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class ReservationsSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    private static ReservationRequest valid() {
        new ReservationRequest("Alice", 12, 4, Instant.now().plus(1, ChronoUnit.DAYS), "555-0100")
    }

    def "a reservation is created, confirmed, and retrievable"() {
        when:
        def created = client.post("/reservations", valid())

        then:
        created.status() == 201
        def reservation = created.as(ReservationResponse)
        reservation.id() > 0
        reservation.status() == "Confirmed"
        client.get("/reservations/${reservation.id()}").status() == 200
    }

    def "retrieving a non-existent reservation returns 404"() {
        expect:
        client.get("/reservations/999999").status() == 404
    }

    def "cancelling twice returns a 409 conflict"() {
        given:
        def reservation = client.post("/reservations", valid()).as(ReservationResponse)

        expect:
        client.patch("/reservations/${reservation.id()}/cancel", "").status() == 200
        client.patch("/reservations/${reservation.id()}/cancel", "").status() == 409
    }

    def "a party size outside 1-20 is rejected"() {
        when:
        def response = client.post("/reservations",
                new ReservationRequest("Alice", 12, 99, Instant.now().plus(1, ChronoUnit.DAYS), null))

        then:
        response.status() == 400
        response.bodyContains("'Party Size' must be between 1 and 20.")
    }

    def "deleting a reservation returns 204"() {
        given:
        def reservation = client.post("/reservations", valid()).as(ReservationResponse)

        expect:
        client.delete("/reservations/${reservation.id()}").status() == 204
    }
}
