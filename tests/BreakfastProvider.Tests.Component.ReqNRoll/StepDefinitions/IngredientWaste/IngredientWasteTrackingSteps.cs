using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;
using BreakfastProvider.Tests.Component.Shared.Models.IngredientWaste;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.IngredientWaste;

[Binding]
public class IngredientWasteTrackingSteps(
    AppManager appManager,
    PostIngredientWasteSteps postSteps,
    GetIngredientWasteSteps getSteps,
    DeleteIngredientWasteSteps deleteSteps)
{
    private string _recipeName = string.Empty;
    private string _createdWasteId = string.Empty;

    [Given("a valid ingredient waste request")]
    public void GivenAValidIngredientWasteRequest()
    {
        _recipeName = $"Pancakes-{Guid.NewGuid():N}";
        postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = $"Butter-{Guid.NewGuid():N}",
            QuantityWasted = 0.5m,
            Unit = "kg",
            RecipeName = _recipeName,
            Reason = "Expired before use"
        };
    }

    [Given("an ingredient waste record has been created")]
    public async Task GivenAnIngredientWasteRecordHasBeenCreated()
    {
        GivenAValidIngredientWasteRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdWasteId = postSteps.Response!.WasteId;
    }

    [Given("a waste request with a missing ingredient name")]
    public void GivenAWasteRequestWithAMissingIngredientName()
    {
        postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = null,
            QuantityWasted = 1.0m,
            Unit = "kg",
            RecipeName = "Pancakes",
            Reason = "Dropped on floor"
        };
    }

    [Given("a waste request with zero quantity")]
    public void GivenAWasteRequestWithZeroQuantity()
    {
        postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Eggs",
            QuantityWasted = 0,
            Unit = "units",
            RecipeName = "Pancakes",
            Reason = "Spoiled"
        };
    }

    [Given("a waste request with a missing reason")]
    public void GivenAWasteRequestWithAMissingReason()
    {
        postSteps.Request = new TestIngredientWasteRequest
        {
            IngredientName = "Milk",
            QuantityWasted = 2.0m,
            Unit = "litres",
            RecipeName = "Pancakes",
            Reason = null
        };
    }

    [When("the waste is recorded")]
    public async Task WhenTheWasteIsRecorded()
    {
        await postSteps.Send();
    }

    [When("the waste is listed by recipe")]
    public async Task WhenTheWasteIsListedByRecipe()
    {
        await getSteps.RetrieveByRecipe(_recipeName);
    }

    [When("the waste record is deleted")]
    public async Task WhenTheWasteRecordIsDeleted()
    {
        await deleteSteps.Delete(_createdWasteId);
    }

    [Then("the waste response should contain the created record")]
    public async Task ThenTheWasteResponseShouldContainTheCreatedRecord()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.IngredientName.Should().NotBeNullOrEmpty();
        postSteps.Response!.QuantityWasted.Should().Be(0.5m);
        postSteps.Response!.WasteId.Should().NotBeNullOrEmpty();
        postSteps.Response!.Reason.Should().Be("Expired before use");
    }

    [Then("the waste list response should contain the record")]
    public async Task ThenTheWasteListResponseShouldContainTheRecord()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(w => w.WasteId == _createdWasteId);
    }

    [Then("the delete response should indicate no content")]
    public void ThenTheDeleteResponseShouldIndicateNoContent()
    {
        deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then("the waste post response should indicate bad request")]
    public void ThenTheWastePostResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
