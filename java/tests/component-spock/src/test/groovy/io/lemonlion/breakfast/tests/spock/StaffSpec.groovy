package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.StaffMemberRequest
import io.lemonlion.breakfast.model.response.StaffMemberResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Staff domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class StaffSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    private static StaffMemberRequest valid() {
        new StaffMemberRequest("Sam Cook", "Chef", "sam@example.com", true, null)
    }

    def "a staff member is created and retrievable"() {
        when:
        def created = client.post("/staff", valid())

        then:
        created.status() == 201
        def staff = created.as(StaffMemberResponse)
        staff.id() > 0
        client.get("/staff/${staff.id()}").status() == 200
    }

    def "an invalid role is rejected"() {
        when:
        def response = client.post("/staff", new StaffMemberRequest("Sam", "Astronaut", "sam@example.com", true, null))

        then:
        response.status() == 400
        response.bodyContains("'Role' must be one of:")
    }

    def "an invalid email is rejected"() {
        when:
        def response = client.post("/staff", new StaffMemberRequest("Sam", "Chef", "not-an-email", true, null))

        then:
        response.status() == 400
        response.bodyContains("'Email' must be a valid email address.")
    }

    def "deleting a staff member returns 204 then 404"() {
        given:
        def staff = client.post("/staff", valid()).as(StaffMemberResponse)

        expect:
        client.delete("/staff/${staff.id()}").status() == 204
        client.delete("/staff/${staff.id()}").status() == 404
    }
}
