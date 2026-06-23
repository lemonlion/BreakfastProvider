package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.BakingProfile
import io.lemonlion.breakfast.model.request.MuffinRequest
import io.lemonlion.breakfast.model.request.MuffinTopping
import io.lemonlion.breakfast.model.response.MuffinResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** Apple-cinnamon Muffins domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class MuffinsSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.kitchen().reset()
    }

    private static MuffinRequest validMuffins() {
        new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon",
                new BakingProfile(180, 25, "Silicone"), [new MuffinTopping("Streusel", "2 tbsp")])
    }

    def "a valid muffin batch bakes with the requested profile"() {
        when:
        def response = client.post("/muffins", validMuffins())

        then:
        response.status() == 201
        def batch = response.as(MuffinResponse)
        batch.ingredients() == ["Whole", "Plain", "Free-range", "Bramley", "Ceylon"]
        batch.bakingTemperature() == 180
    }

    def "a muffin request without apples is rejected"() {
        when:
        def response = client.post("/muffins",
                new MuffinRequest("Whole", "Plain", "Free-range", null, "Ceylon", new BakingProfile(180, 25, "Silicone"), []))

        then:
        response.status() == 400
        response.bodyContains("'Apples' is required.")
    }

    def "a baking temperature outside 150-220 is rejected"() {
        when:
        def response = client.post("/muffins",
                new MuffinRequest("Whole", "Plain", "Free-range", "Bramley", "Ceylon", new BakingProfile(300, 25, "Silicone"), []))

        then:
        response.status() == 400
        response.bodyContains("Baking temperature must be between 150 and 220 degrees.")
    }
}
