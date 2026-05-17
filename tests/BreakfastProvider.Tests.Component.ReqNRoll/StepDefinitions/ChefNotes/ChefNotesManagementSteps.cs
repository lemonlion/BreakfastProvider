using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.ChefNotes;
using BreakfastProvider.Tests.Component.Shared.Models.ChefNotes;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.ChefNotes;

[Binding]
public class ChefNotesManagementSteps(
    AppManager appManager,
    PostChefNoteSteps postSteps,
    GetChefNoteSteps getSteps,
    PatchChefNoteSteps patchSteps)
{
    private string _recipeName = string.Empty;
    private string _createdNoteId = string.Empty;

    [Given("a valid chef note request")]
    public void GivenAValidChefNoteRequest()
    {
        _recipeName = $"Recipe-{Guid.NewGuid():N}";
        postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = _recipeName,
            ChefName = $"Chef-{Guid.NewGuid():N}",
            NoteText = "Remember to fold the batter gently to keep it fluffy.",
            Category = "Technique"
        };
    }

    [Given("a chef note exists")]
    public async Task GivenAChefNoteExists()
    {
        GivenAValidChefNoteRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdNoteId = postSteps.Response!.NoteId;
    }

    [Given("a note request with a missing recipe name")]
    public void GivenANoteRequestWithAMissingRecipeName()
    {
        postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = null,
            ChefName = "Chef John",
            NoteText = "Use room temperature eggs.",
            Category = "Ingredients"
        };
    }

    [Given("a note request with missing note text")]
    public void GivenANoteRequestWithMissingNoteText()
    {
        postSteps.Request = new TestChefNoteRequest
        {
            RecipeName = "Pancakes",
            ChefName = "Chef John",
            NoteText = null,
            Category = "Tips"
        };
    }

    [When("the note is submitted")]
    public async Task WhenTheNoteIsSubmitted()
    {
        await postSteps.Send();
    }

    [When("the note is retrieved by id")]
    public async Task WhenTheNoteIsRetrievedById()
    {
        await getSteps.RetrieveById(_createdNoteId);
    }

    [When("the note is updated")]
    public async Task WhenTheNoteIsUpdated()
    {
        patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "Updated: Use unsalted butter for better flavour control.",
            Category = "Ingredients"
        };
        await patchSteps.Send(_createdNoteId);
    }

    [When("the notes are listed by recipe")]
    public async Task WhenTheNotesAreListedByRecipe()
    {
        await getSteps.RetrieveByRecipe(_recipeName);
    }

    [When("a non-existent note is retrieved")]
    public async Task WhenANonExistentNoteIsRetrieved()
    {
        await getSteps.RetrieveById(Guid.NewGuid().ToString());
    }

    [When("a non-existent note is updated")]
    public async Task WhenANonExistentNoteIsUpdated()
    {
        patchSteps.Request = new TestUpdateChefNoteRequest
        {
            NoteText = "This should fail.",
            Category = "Tips"
        };
        await patchSteps.Send(Guid.NewGuid().ToString());
    }

    [Then("the response should contain the created note")]
    public async Task ThenTheResponseShouldContainTheCreatedNote()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.RecipeName.Should().Be(_recipeName);
        postSteps.Response!.NoteText.Should().Be("Remember to fold the batter gently to keep it fluffy.");
        postSteps.Response!.Category.Should().Be("Technique");
        postSteps.Response!.NoteId.Should().NotBeNullOrEmpty();
    }

    [Then("the get response should contain the note")]
    public async Task ThenTheGetResponseShouldContainTheNote()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.NoteId.Should().Be(_createdNoteId);
        getSteps.Response!.RecipeName.Should().Be(_recipeName);
    }

    [Then("the update response should contain the modified note")]
    public async Task ThenTheUpdateResponseShouldContainTheModifiedNote()
    {
        patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await patchSteps.ParseResponse();
        patchSteps.Response!.NoteId.Should().Be(_createdNoteId);
        patchSteps.Response!.NoteText.Should().Be("Updated: Use unsalted butter for better flavour control.");
        patchSteps.Response!.Category.Should().Be("Ingredients");
        patchSteps.Response!.UpdatedAt.Should().NotBeNull();
    }

    [Then("the list response should contain the note")]
    public async Task ThenTheListResponseShouldContainTheNote()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(n => n.NoteId == _createdNoteId);
    }

    [Then("the get response should indicate not found")]
    public void ThenTheGetResponseShouldIndicateNotFound()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then("the update response should indicate not found")]
    public void ThenTheUpdateResponseShouldIndicateNotFound()
    {
        patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Then("the note response should indicate bad request")]
    public void ThenTheNoteResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
