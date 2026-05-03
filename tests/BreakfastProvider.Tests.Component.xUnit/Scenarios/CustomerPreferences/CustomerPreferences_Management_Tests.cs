using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.CustomerPreferences;

public class CustomerPreferences_Management_Tests : BaseFixture
{
    private readonly PutCustomerPreferenceSteps _putSteps;
    private readonly GetCustomerPreferenceSteps _getSteps;

    public CustomerPreferences_Management_Tests()
    {
        _putSteps = Get<PutCustomerPreferenceSteps>();
        _getSteps = Get<GetCustomerPreferenceSteps>();
    }

    [Fact]
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
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _putSteps.ParseResponse();
        Track.That(() => _putSteps.Response!.PreferredMilkType.Should().Be("Oat"));
        Track.That(() => _putSteps.Response!.FavouriteItem.Should().Be("Blueberry Pancakes"));
    }

    [Fact]
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
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _putSteps.ParseResponse();

        // When the customer preferences are retrieved
        await _getSteps.RetrieveById(customerId);

        // Then the response should contain the preferences
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseResponse();
        Track.That(() => _getSteps.Response!.CustomerId.Should().Be(customerId));
        Track.That(() => _getSteps.Response!.PreferredMilkType.Should().Be("Oat"));
        Track.That(() => _getSteps.Response!.LikesExtraToppings.Should().BeTrue());
    }

    [Fact]
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
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
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
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _putSteps.ParseResponse();
        Track.That(() => _putSteps.Response!.PreferredMilkType.Should().Be("Almond"));
        Track.That(() => _putSteps.Response!.FavouriteItem.Should().Be("Belgian Waffles"));
    }

    [Fact]
    public async Task Retrieving_non_existent_customer_preferences_should_return_not_found()
    {
        // When non-existent customer preferences are retrieved
        await _getSteps.RetrieveById(Guid.NewGuid().ToString("N"));

        // Then the response should indicate not found
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }

    [Fact]
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
        Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest));
    }
}
