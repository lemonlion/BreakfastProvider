using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.ChefNotes;

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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.RecipeName.Should().Be(recipeName);
        _postSteps.Response!.NoteText.Should().Be("Remember to fold the batter gently to keep it fluffy.");
        _postSteps.Response!.Category.Should().Be("Technique");
        _postSteps.Response!.NoteId.Should().NotBeNullOrEmpty();
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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdNoteId = _postSteps.Response!.NoteId;

        // When the note is retrieved by id
        await _getSteps.RetrieveById(createdNoteId);

        // Then the response should contain the note
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.NoteId.Should().Be(createdNoteId);
        _getSteps.Response!.RecipeName.Should().Be(recipeName);
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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
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
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _patchSteps.ParseResponse();
        _patchSteps.Response!.NoteId.Should().Be(createdNoteId);
        _patchSteps.Response!.NoteText.Should().Be("Updated: Use unsalted butter for better flavour control.");
        _patchSteps.Response!.Category.Should().Be("Ingredients");
        _patchSteps.Response!.UpdatedAt.Should().NotBeNull();
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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdNoteId = _postSteps.Response!.NoteId;

        // When the notes are listed by recipe
        await _getSteps.RetrieveByRecipe(recipeName);

        // Then the list should contain the note
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(n => n.NoteId == createdNoteId);
    }

    [Test]
    public async Task Retrieving_a_non_existent_note_should_return_not_found()
    {
        // When a non-existent note is retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString());

        // Then the response should indicate not found
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
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
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
