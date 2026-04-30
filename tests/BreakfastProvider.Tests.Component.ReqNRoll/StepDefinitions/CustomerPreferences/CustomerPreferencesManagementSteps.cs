using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.CustomerPreferences;

[Binding]
public class CustomerPreferencesManagementSteps(
    PutCustomerPreferenceSteps putSteps,
    GetCustomerPreferenceSteps getSteps)
{
    private string _customerId = string.Empty;

    [Given("a valid customer preference request")]
    public void GivenAValidCustomerPreferenceRequest()
    {
        _customerId = Guid.NewGuid().ToString("N");
        putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            PreferredMilkType = "Oat",
            LikesExtraToppings = true,
            FavouriteItem = "Blueberry Pancakes"
        };
    }

    [Given("customer preferences exist")]
    public async Task GivenCustomerPreferencesExist()
    {
        GivenAValidCustomerPreferenceRequest();
        await putSteps.Send(_customerId);
        putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await putSteps.ParseResponse();
    }

    [Given("a customer preference request with missing customer name")]
    public void GivenACustomerPreferenceRequestWithMissingCustomerName()
    {
        _customerId = Guid.NewGuid().ToString("N");
        putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = null,
            PreferredMilkType = "Oat",
            LikesExtraToppings = false,
            FavouriteItem = "Waffles"
        };
    }

    [When("the customer preferences are saved")]
    public async Task WhenTheCustomerPreferencesAreSaved() => await putSteps.Send(_customerId);

    [When("the customer preferences are retrieved")]
    public async Task WhenTheCustomerPreferencesAreRetrieved() => await getSteps.RetrieveById(_customerId);

    [When("the customer preferences are updated")]
    public async Task WhenTheCustomerPreferencesAreUpdated()
    {
        putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = putSteps.Response!.CustomerName,
            PreferredMilkType = "Almond",
            LikesExtraToppings = false,
            FavouriteItem = "Belgian Waffles"
        };
        await putSteps.Send(_customerId);
    }

    [When("non-existent customer preferences are retrieved")]
    public async Task WhenNonExistentCustomerPreferencesAreRetrieved()
        => await getSteps.RetrieveById(Guid.NewGuid().ToString("N"));

    [Then("the preference response should contain the saved preferences")]
    public async Task ThenThePreferenceResponseShouldContainTheSavedPreferences()
    {
        putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await putSteps.ParseResponse();
        putSteps.Response!.PreferredMilkType.Should().Be("Oat");
        putSteps.Response!.FavouriteItem.Should().Be("Blueberry Pancakes");
    }

    [Then("the preference get response should contain the preferences")]
    public async Task ThenThePreferenceGetResponseShouldContainThePreferences()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseResponse();
        getSteps.Response!.CustomerId.Should().Be(_customerId);
        getSteps.Response!.PreferredMilkType.Should().Be("Oat");
        getSteps.Response!.LikesExtraToppings.Should().BeTrue();
    }

    [Then("the preference update response should contain the updated values")]
    public async Task ThenThePreferenceUpdateResponseShouldContainTheUpdatedValues()
    {
        putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await putSteps.ParseResponse();
        putSteps.Response!.PreferredMilkType.Should().Be("Almond");
        putSteps.Response!.FavouriteItem.Should().Be("Belgian Waffles");
    }

    [Then("the preference get response should indicate not found")]
    public void ThenThePreferenceGetResponseShouldIndicateNotFound()
        => getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    [Then("the preference response should indicate bad request")]
    public void ThenThePreferenceResponseShouldIndicateBadRequest()
        => putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
}
