package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** ChefNotes domain component tests (JUnit 5) — MongoDB persistence. */
@DisplayName("ChefNotes")
class ChefNotesComponentTest extends ComponentTestBase {

    private static ChefNoteRequest valid() {
        return new ChefNoteRequest("Classic Pancakes", "Chef Remy", "Rest the batter for 10 minutes.", "Technique");
    }

    @Test
    @DisplayName("a chef note is created and retrievable by id")
    void createAndRetrieve() {
        TestResponse created = client.post("/chef-notes", valid());
        assertThat(created.status()).isEqualTo(201);
        ChefNoteResponse note = created.as(ChefNoteResponse.class);
        assertThat(note.noteId()).isNotBlank();

        TestResponse fetched = client.get("/chef-notes/" + note.noteId());
        assertThat(fetched.status()).isEqualTo(200);
        assertThat(fetched.as(ChefNoteResponse.class).chefName()).isEqualTo("Chef Remy");
    }

    @Test
    @DisplayName("a chef note can be updated")
    void updateNote() {
        ChefNoteResponse note = client.post("/chef-notes", valid()).as(ChefNoteResponse.class);
        TestResponse updated = client.patch("/chef-notes/" + note.noteId(),
                new UpdateChefNoteRequest("Rest the batter for 20 minutes.", "Technique"));
        assertThat(updated.status()).isEqualTo(200);
        assertThat(updated.as(ChefNoteResponse.class).noteText()).contains("20 minutes");
    }

    @Test
    @DisplayName("retrieving a non-existent note returns 404")
    void getMissing() {
        assertThat(client.get("/chef-notes/does-not-exist").status()).isEqualTo(404);
    }

    @Test
    @DisplayName("an empty recipe name is rejected")
    void rejectsEmptyRecipe() {
        TestResponse response = client.post("/chef-notes",
                new ChefNoteRequest("", "Chef Remy", "Some note", "Technique"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Recipe Name' must not be empty.")).isTrue();
    }
}
