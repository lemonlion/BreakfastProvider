using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.ChefNotes;

public class ChefNotes_Management_Tests : BaseFixture
{
    private readonly PostChefNoteSteps _postSteps;
    private readonly GetChefNoteSteps _getSteps;
    private readonly PatchChefNoteSteps _patchSteps;

    public ChefNotes_Management_Tests()
    {
        _postSteps = Get<PostChefNoteSteps>();
        _getSteps = Get<GetChefNoteSteps>();
        _patchSteps = Get<PatchChefNoteSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Creating_a_chef_note_should_return_the_created_note()
    {
        // Given a valid chef note request
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };

        // When the note is submitted
        await _postSteps.Send();

        // Then the response should contain the created note
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        await _postSteps.Response!.RecipeName.Should().BeEqualTo(recipeName);
        await _postSteps.Response!.NoteText.Should().BeEqualTo("Remember to fold the batter gently to keep it fluffy.");
        await _postSteps.Response!.Category.Should().BeEqualTo("Technique");
        await _postSteps.Response!.NoteId.Should().NotBeNull();
    }

    [Test]
    public async Task Retrieving_an_existing_note_by_id_should_return_the_note()
    {
        // Given a chef note exists
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdNoteId = _postSteps.Response!.NoteId;

        // When the note is retrieved by id
        await _getSteps.RetrieveById(createdNoteId);

        // Then the response should contain the note
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        await _getSteps.Response!.NoteId.Should().BeEqualTo(createdNoteId);
        await _getSteps.Response!.RecipeName.Should().BeEqualTo(recipeName);
    }

    [Test]
    public async Task Updating_an_existing_note_should_return_the_updated_note()
    {
        // Given a chef note exists
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Original note text.",
            Category = "Technique"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdNoteId = _postSteps.Response!.NoteId;

        // When the note is updated
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "Updated: Use unsalted butter for better flavour control.",
            Category = "Ingredients"
        };
        await _patchSteps.Send(createdNoteId);

        // Then the response should contain the modified note
        await _patchSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _patchSteps.ParseResponse();
        await _patchSteps.Response!.NoteId.Should().BeEqualTo(createdNoteId);
        await _patchSteps.Response!.NoteText.Should().BeEqualTo("Updated: Use unsalted butter for better flavour control.");
        await _patchSteps.Response!.Category.Should().BeEqualTo("Ingredients");
        await _patchSteps.Response!.UpdatedAt.HasValue.Should().BeTrue();
    }

    [Test]
    public async Task Listing_notes_by_recipe_should_return_matching_notes()
    {
        // Given a chef note exists
        var recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdNoteId = _postSteps.Response!.NoteId;

        // When the notes are listed by recipe
        await _getSteps.RetrieveByRecipe(recipeName);

        // Then the list should contain the note
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        await _getSteps.ListResponse!.Should().Contain(n => n.NoteId == createdNoteId);
    }

    [Test]
    public async Task Retrieving_a_non_existent_note_should_return_not_found()
    {
        // When a non-existent note is retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());

        // Then the response should indicate not found
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Updating_a_non_existent_note_should_return_not_found()
    {
        // When a non-existent note is updated
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "This should fail.",
            Category = "Tips"
        };
        await _patchSteps.Send(Guid.NewGuid().ToString());

        // Then the response should indicate not found
        await _patchSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Creating_a_note_with_missing_recipe_name_should_return_bad_request()
    {
        // Given a note request with missing recipe name
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = null,
            ChefName = "Chef John",
            NoteText = "Use room temperature eggs.",
            Category = "Ingredients"
        };

        // When the note is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Creating_a_note_with_missing_note_text_should_return_bad_request()
    {
        // Given a note request with missing note text
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = "Pancakes",
            ChefName = "Chef John",
            NoteText = null,
            Category = "Tips"
        };

        // When the note is submitted
        await _postSteps.Send();

        // Then the response should indicate bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }
}
