package io.lemonlion.breakfast.tests.junit5;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import java.util.UUID;
import org.junit.jupiter.api.DisplayName;
import org.junit.jupiter.api.Test;

/** ChefNotes domain component tests (JUnit 5) — MongoDB persistence. */
@DisplayName("ChefNotes")
class ChefNotesComponentTest extends ComponentTestBase {

    private static ChefNoteRequest valid() {
        return new ChefNoteRequest("Classic Pancakes", "Chef Remy", "Rest the batter for 10 minutes.", "Technique");
    }

    @Test
    @DisplayName("creating a chef note returns the created note")
    void createReturnsNote() {
        TestResponse created = client.post("/chef-notes", valid());
        assertThat(created.status()).isEqualTo(201);
        ChefNoteResponse note = created.as(ChefNoteResponse.class);
        assertThat(note.noteId()).isNotBlank();
        assertThat(note.chefName()).isEqualTo("Chef Remy");
    }

    @Test
    @DisplayName("an existing chef note is retrievable by id")
    void retrieveById() {
        ChefNoteResponse note = client.post("/chef-notes", valid()).as(ChefNoteResponse.class);

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

    @Test
    @DisplayName("notes are listed by recipe")
    void listByRecipe() {
        String recipe = "ChefRecipe" + UUID.randomUUID().toString().replace("-", "");
        client.post("/chef-notes", new ChefNoteRequest(recipe, "Chef Remy", "Use a hot pan.", "Technique"));

        TestResponse byRecipe = client.get("/chef-notes/recipe/" + recipe);

        assertThat(byRecipe.status()).isEqualTo(200);
        assertThat(byRecipe.bodyContains("Use a hot pan.")).isTrue();
    }

    @Test
    @DisplayName("updating a non-existent note returns 404")
    void updateMissing() {
        TestResponse response = client.patch("/chef-notes/does-not-exist",
                new UpdateChefNoteRequest("Updated", "Technique"));
        assertThat(response.status()).isEqualTo(404);
    }

    @Test
    @DisplayName("a note without text is rejected")
    void rejectsEmptyNoteText() {
        TestResponse response = client.post("/chef-notes",
                new ChefNoteRequest("Classic Pancakes", "Chef Remy", "", "Technique"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Note Text' must not be empty.")).isTrue();
    }
}
