using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.CustomerPreferences;

public class CustomerPreferences_Management_Tests : BaseFixture
{
    private readonly PutCustomerPreferenceSteps _putSteps;
    private readonly GetCustomerPreferenceSteps _getSteps;

    public CustomerPreferences_Management_Tests()
    {
        _putSteps = Get<PutCustomerPreferenceSteps>();
        _getSteps = Get<GetCustomerPreferenceSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Saving_customer_preferences_should_return_the_saved_preferences()
    {
        // Given a valid customer preference request
        var customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = customerId,
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            PreferredMilkType = "Oat",
            LikesExtraToppings = true,
            FavouriteItem = "Blueberry Pancakes"
        };

        // When the customer preferences are saved
        await _putSteps.Send(customerId);

        // Then the response should contain the saved preferences
        await _putSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        await _putSteps.Response!.PreferredMilkType.Should().BeEqualTo("Oat");
        await _putSteps.Response!.FavouriteItem.Should().BeEqualTo("Blueberry Pancakes");
    }

    [Test]
    public async Task Retrieving_existing_customer_preferences_should_return_the_preferences()
    {
        // Given customer preferences exist
        var customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = customerId,
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            PreferredMilkType = "Oat",
            LikesExtraToppings = true,
            FavouriteItem = "Blueberry Pancakes"
        };
        await _putSteps.Send(customerId);
        await _putSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _putSteps.ParseResponse();

        // When the customer preferences are retrieved
        await _getSteps.RetrieveById(customerId);

        // Then the response should contain the preferences
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        await _getSteps.Response!.CustomerId.Should().BeEqualTo(customerId);
        await _getSteps.Response!.PreferredMilkType.Should().BeEqualTo("Oat");
        await _getSteps.Response!.LikesExtraToppings.Should().BeTrue();
    }

    [Test]
    public async Task Updating_customer_preferences_should_return_the_updated_preferences()
    {
        // Given customer preferences exist
        var customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = customerId,
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            PreferredMilkType = "Oat",
            LikesExtraToppings = true,
            FavouriteItem = "Blueberry Pancakes"
        };
        await _putSteps.Send(customerId);
        await _putSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _putSteps.ParseResponse();

        // When the customer preferences are updated
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = customerId,
            CustomerName = _putSteps.Response!.CustomerName,
            PreferredMilkType = "Almond",
            LikesExtraToppings = false,
            FavouriteItem = "Belgian Waffles"
        };
        await _putSteps.Send(customerId);

        // Then the response should contain the updated values
        await _putSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        await _putSteps.Response!.PreferredMilkType.Should().BeEqualTo("Almond");
        await _putSteps.Response!.FavouriteItem.Should().BeEqualTo("Belgian Waffles");
    }

    [Test]
    public async Task Retrieving_non_existent_customer_preferences_should_return_not_found()
    {
        // When non-existent customer preferences are retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString("N"));

        // Then the response should indicate not found
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Saving_customer_preferences_with_missing_customer_name_should_return_bad_request()
    {
        // Given a customer preference request with missing customer name
        var customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = customerId,
            CustomerName = null,
            PreferredMilkType = "Oat",
            LikesExtraToppings = false,
            FavouriteItem = "Waffles"
        };

        // When the customer preferences are saved
        await _putSteps.Send(customerId);

        // Then the response should indicate bad request
        await _putSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }
}
