package io.lemonlion.breakfast.tests.spock

import io.lemonlion.breakfast.BreakfastProviderApplication
import io.lemonlion.breakfast.model.request.ChefNoteRequest
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest
import io.lemonlion.breakfast.model.response.ChefNoteResponse
import io.lemonlion.breakfast.testsupport.BackendsInitializer
import io.lemonlion.breakfast.testsupport.BreakfastBackends
import io.lemonlion.breakfast.testsupport.BreakfastTestClient
import org.springframework.boot.test.context.SpringBootTest
import org.springframework.boot.test.web.server.LocalServerPort
import org.springframework.test.context.ContextConfiguration
import spock.lang.Specification

/** ChefNotes domain component spec (Spock). */
@SpringBootTest(classes = BreakfastProviderApplication, webEnvironment = SpringBootTest.WebEnvironment.RANDOM_PORT)
@ContextConfiguration(initializers = BackendsInitializer)
class ChefNotesSpec extends Specification {

    @LocalServerPort
    int port

    BreakfastTestClient client

    def setup() {
        client = new BreakfastTestClient("http://127.0.0.1:${port}")
        BreakfastBackends.resetFakes()
    }

    private static ChefNoteRequest valid() {
        new ChefNoteRequest("Classic Pancakes", "Chef Remy", "Rest the batter for 10 minutes.", "Technique")
    }

    def "a chef note is created and retrievable by id"() {
        when:
        def created = client.post("/chef-notes", valid())

        then:
        created.status() == 201
        def note = created.as(ChefNoteResponse)
        note.noteId()
        client.get("/chef-notes/${note.noteId()}").status() == 200
    }

    def "a chef note can be updated"() {
        given:
        def note = client.post("/chef-notes", valid()).as(ChefNoteResponse)

        when:
        def updated = client.patch("/chef-notes/${note.noteId()}", new UpdateChefNoteRequest("Rest the batter for 20 minutes.", "Technique"))

        then:
        updated.status() == 200
        updated.as(ChefNoteResponse).noteText().contains("20 minutes")
    }

    def "an empty recipe name is rejected"() {
        when:
        def response = client.post("/chef-notes", new ChefNoteRequest("", "Chef Remy", "Some note", "Technique"))

        then:
        response.status() == 400
        response.bodyContains("'Recipe Name' must not be empty.")
    }
}
