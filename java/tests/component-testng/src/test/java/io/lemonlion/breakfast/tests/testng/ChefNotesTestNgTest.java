package io.lemonlion.breakfast.tests.testng;

import static org.assertj.core.api.Assertions.assertThat;

import io.lemonlion.breakfast.model.request.ChefNoteRequest;
import io.lemonlion.breakfast.model.request.UpdateChefNoteRequest;
import io.lemonlion.breakfast.model.response.ChefNoteResponse;
import io.lemonlion.breakfast.testsupport.TestResponse;
import org.testng.annotations.Test;

/** ChefNotes domain component tests (TestNG). */
public class ChefNotesTestNgTest extends ComponentTestBaseNg {

    private static ChefNoteRequest valid() {
        return new ChefNoteRequest("Classic Pancakes", "Chef Remy", "Rest the batter for 10 minutes.", "Technique");
    }

    @Test
    public void createAndRetrieve() {
        TestResponse created = client.post("/chef-notes", valid());
        assertThat(created.status()).isEqualTo(201);
        ChefNoteResponse note = created.as(ChefNoteResponse.class);
        assertThat(note.noteId()).isNotBlank();
        assertThat(client.get("/chef-notes/" + note.noteId()).status()).isEqualTo(200);
    }

    @Test
    public void updateNote() {
        ChefNoteResponse note = client.post("/chef-notes", valid()).as(ChefNoteResponse.class);
        TestResponse updated = client.patch("/chef-notes/" + note.noteId(),
                new UpdateChefNoteRequest("Rest the batter for 20 minutes.", "Technique"));
        assertThat(updated.status()).isEqualTo(200);
    }

    @Test
    public void getMissing() {
        assertThat(client.get("/chef-notes/does-not-exist").status()).isEqualTo(404);
    }

    @Test
    public void rejectsEmptyRecipe() {
        TestResponse response = client.post("/chef-notes",
                new ChefNoteRequest("", "Chef Remy", "Some note", "Technique"));
        assertThat(response.status()).isEqualTo(400);
        assertThat(response.bodyContains("'Recipe Name' must not be empty.")).isTrue();
    }
}
