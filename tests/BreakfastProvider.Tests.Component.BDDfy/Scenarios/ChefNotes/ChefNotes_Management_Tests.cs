using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.ChefNotes;

public class ChefNotes_Management_Tests : BaseFixture
{
    private readonly PostChefNoteSteps _postSteps;
    private readonly GetChefNoteSteps _getSteps;
    private readonly PatchChefNoteSteps _patchSteps;

    private string _recipeName = null!;
    private string _createdNoteId = null!;

    public ChefNotes_Management_Tests()
    {
        _postSteps = Get<PostChefNoteSteps>();
        _getSteps = Get<GetChefNoteSteps>();
        _patchSteps = Get<PatchChefNoteSteps>();
    }

    [Fact]
    [HappyPath]
    public void Creating_a_chef_note_should_return_the_created_note()
    {
        this.Given(x => x.A_valid_chef_note_request_is_prepared())
            .When(x => x.The_note_is_submitted())
            .Then(x => x.The_response_should_contain_the_created_note())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_an_existing_note_by_id_should_return_the_note()
    {
        this.Given(x => x.A_chef_note_exists())
            .When(x => x.The_note_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_note())
            .BDDfy();
    }

    [Fact]
    public void Updating_an_existing_note_should_return_the_updated_note()
    {
        this.Given(x => x.A_chef_note_exists())
            .When(x => x.The_note_is_updated())
            .Then(x => x.The_update_response_should_contain_the_modified_note())
            .BDDfy();
    }

    [Fact]
    public void Listing_notes_by_recipe_should_return_matching_notes()
    {
        this.Given(x => x.A_chef_note_exists())
            .When(x => x.The_notes_are_listed_by_recipe())
            .Then(x => x.The_list_response_should_contain_the_note())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_a_non_existent_note_should_return_not_found()
    {
        this.When(x => x.A_non_existent_note_is_retrieved())
            .Then(x => x.The_get_response_should_indicate_not_found())
            .BDDfy();
    }

    [Fact]
    public void Updating_a_non_existent_note_should_return_not_found()
    {
        this.When(x => x.A_non_existent_note_is_updated())
            .Then(x => x.The_update_response_should_indicate_not_found())
            .BDDfy();
    }

    [Fact]
    public void Creating_a_note_with_missing_recipe_name_should_return_bad_request()
    {
        this.Given(x => x.A_note_request_with_missing_recipe_name_is_prepared())
            .When(x => x.The_note_is_submitted())
            .Then(x => x.The_note_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Creating_a_note_with_missing_note_text_should_return_bad_request()
    {
        this.Given(x => x.A_note_request_with_missing_note_text_is_prepared())
            .When(x => x.The_note_is_submitted())
            .Then(x => x.The_note_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_chef_note_request_is_prepared()
    {
        _recipeName = $"Recipe-{Guid.NewGuid():N}";
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = _recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };
        await Task.CompletedTask;
    }

    private async Task The_note_is_submitted() => await _postSteps.Send();

    private async Task The_response_should_contain_the_created_note()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.RecipeName.Should().Be(_recipeName);
        _postSteps.Response!.NoteText.Should().Be("Remember to fold the batter gently to keep it fluffy.");
        _postSteps.Response!.Category.Should().Be("Technique");
        _postSteps.Response!.NoteId.Should().NotBeNullOrEmpty();
    }

    private async Task A_chef_note_exists()
    {
        await A_valid_chef_note_request_is_prepared();
        await The_note_is_submitted();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdNoteId = _postSteps.Response!.NoteId;
    }

    private async Task The_note_is_retrieved_by_id() => await _getSteps.RetrieveById(_createdNoteId);

    private async Task The_get_response_should_contain_the_note()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.NoteId.Should().Be(_createdNoteId);
        _getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    private async Task The_note_is_updated()
    {
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "Updated: Use unsalted butter for better flavour control.",
            Category = "Ingredients"
        };
        await _patchSteps.Send(_createdNoteId);
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

    private async Task The_notes_are_listed_by_recipe() => await _getSteps.RetrieveByRecipe(_recipeName);

    private async Task The_list_response_should_contain_the_note()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(n => n.NoteId == _createdNoteId);
    }

    private async Task A_non_existent_note_is_retrieved() => await _getSteps.RetrieveById(Guid.NewGuid().ToString());

    private void The_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task A_non_existent_note_is_updated()
    {
        _patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "This should fail.",
            Category = "Tips"
        };
        await _patchSteps.Send(Guid.NewGuid().ToString());
    }

    private void The_update_response_should_indicate_not_found()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task A_note_request_with_missing_recipe_name_is_prepared()
    {
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = null,
            ChefName = "Chef John",
            NoteText = "Use room temperature eggs.",
            Category = "Ingredients"
        };
        await Task.CompletedTask;
    }

    private async Task A_note_request_with_missing_note_text_is_prepared()
    {
        _postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = "Pancakes",
            ChefName = "Chef John",
            NoteText = null,
            Category = "Tips"
        };
        await Task.CompletedTask;
    }

    private void The_note_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
