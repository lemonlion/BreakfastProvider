using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.ChefNotes;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Chef_Notes__Management_Feature : BaseFixture
{
    private readonly PostChefNoteSteps _postSteps;
    private readonly GetChefNoteSteps _getSteps;
    private readonly PatchChefNoteSteps _patchSteps;
    private string _createdNoteId = string.Empty;
    private string _recipeName = string.Empty;

    public Chef_Notes__Management_Feature()
    {
        _postSteps = Get<PostChefNoteSteps>();
        _getSteps = Get<GetChefNoteSteps>();
        _patchSteps = Get<PatchChefNoteSteps>();
    }

    #region Given

    private async Task A_valid_chef_note_request()
    {
        _recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = _recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };
    }

    private async Task<CompositeStep> A_chef_note_exists()
    {
        return Sub.Steps(
            _ => A_valid_chef_note_request(),
            _ => The_note_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdNoteId = _postSteps.Response!.NoteId;
    }

    private async Task A_note_request_with_missing_recipe_name()
    {
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = null,
            ChefName = "Chef John",
            NoteText = "Use room temperature eggs.",
            Category = "Ingredients"
        };
    }

    private async Task A_note_request_with_missing_note_text()
    {
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = "Pancakes",
            ChefName = "Chef John",
            NoteText = null,
            Category = "Tips"
        };
    }

    #endregion

    #region When

    private async Task The_note_is_submitted() => await _postSteps.Send();

    private async Task The_note_is_retrieved_by_id() => await _getSteps.RetrieveById(_createdNoteId);

    private async Task The_note_is_updated()
    {
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "Updated: Use unsalted butter for better flavour control.",
            Category = "Ingredients"
        };
        await _patchSteps.Send(_createdNoteId);
    }

    private async Task The_notes_are_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task A_non_existent_note_is_retrieved() => await _getSteps.RetrieveById(Guid.NewGuid().ToString());

    private async Task A_non_existent_note_is_updated()
    {
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "This should fail.",
            Category = "Tips"
        };
        await _patchSteps.Send(Guid.NewGuid().ToString());
    }

    #endregion

    #region Then

    private async Task The_response_should_contain_the_created_note()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.RecipeName.Should().Be(_recipeName);
        _postSteps.Response!.NoteText.Should().Be("Remember to fold the batter gently to keep it fluffy.");
        _postSteps.Response!.Category.Should().Be("Technique");
        _postSteps.Response!.NoteId.Should().NotBeNullOrEmpty();
    }

    private async Task The_get_response_should_contain_the_note()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.NoteId.Should().Be(_createdNoteId);
        _getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    private async Task The_update_response_should_contain_the_modified_note()
    {
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _patchSteps.ParseResponse();
        _patchSteps.Response!.NoteId.Should().Be(_createdNoteId);
        _patchSteps.Response!.NoteText.Should().Be("Updated: Use unsalted butter for better flavour control.");
        _patchSteps.Response!.Category.Should().Be("Ingredients");
        _patchSteps.Response!.UpdatedAt.Should().NotBeNull();
    }

    private async Task The_list_response_should_contain_the_note()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(n => n.NoteId == _createdNoteId);
    }

    private async Task The_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_update_response_should_indicate_not_found()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_note_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
