using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.CustomerPreferences;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Customer_Preferences__Management_Feature : BaseFixture
{
    private readonly PutCustomerPreferenceSteps _putSteps;
    private readonly GetCustomerPreferenceSteps _getSteps;
    private string _customerId = string.Empty;

    public Customer_Preferences__Management_Feature()
    {
        _putSteps = Get<PutCustomerPreferenceSteps>();
        _getSteps = Get<GetCustomerPreferenceSteps>();
    }

    #region Given

    private async Task A_valid_customer_preference_request()
    {
        _customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            PreferredMilkType = "Oat",
            LikesExtraToppings = true,
            FavouriteItem = "Blueberry Pancakes"
        };
    }

    private async Task<CompositeStep> Customer_preferences_exist()
    {
        return Sub.Steps(
            _ => A_valid_customer_preference_request(),
            _ => The_customer_preferences_are_saved(),
            _ => The_setup_response_should_be_ok());
    }

    private async Task The_setup_response_should_be_ok()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
    }

    private async Task A_customer_preference_request_with_missing_customer_name()
    {
        _customerId = Guid.NewGuid().ToString("N");
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = null,
            PreferredMilkType = "Oat",
            LikesExtraToppings = false,
            FavouriteItem = "Waffles"
        };
    }

    #endregion

    #region When

    private async Task The_customer_preferences_are_saved()
        => await _putSteps.Send(_customerId);

    private async Task The_customer_preferences_are_retrieved()
        => await _getSteps.RetrieveById(_customerId);

    private async Task The_customer_preferences_are_updated()
    {
        _putSteps.Request = new TestCustomerPreferenceRequest
        {
            CustomerId = _customerId,
            CustomerName = _putSteps.Response!.CustomerName,
            PreferredMilkType = "Almond",
            LikesExtraToppings = false,
            FavouriteItem = "Belgian Waffles"
        };
        await _putSteps.Send(_customerId);
    }

    private async Task Non_existent_customer_preferences_are_retrieved()
        => await _getSteps.RetrieveById(Guid.NewGuid().ToString("N"));

    #endregion

    #region Then

    private async Task<CompositeStep> The_preference_response_should_contain_the_saved_preferences()
    {
        return Sub.Steps(
            _ => The_put_response_http_status_should_be_ok(),
            _ => The_put_response_should_be_valid_json(),
            _ => The_saved_preferences_should_have_the_correct_milk_type(),
            _ => The_saved_preferences_should_have_the_correct_favourite_item());
    }

    private async Task The_put_response_http_status_should_be_ok()
        => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_put_response_should_be_valid_json()
        => await _putSteps.ParseResponse();

    private async Task The_saved_preferences_should_have_the_correct_milk_type()
        => _putSteps.Response!.PreferredMilkType.Should().Be("Oat");

    private async Task The_saved_preferences_should_have_the_correct_favourite_item()
        => _putSteps.Response!.FavouriteItem.Should().Be("Blueberry Pancakes");

    private async Task<CompositeStep> The_preference_get_response_should_contain_the_preferences()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_get_response_should_be_valid_json(),
            _ => The_retrieved_preferences_should_match_the_saved_preferences());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_get_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_retrieved_preferences_should_match_the_saved_preferences()
    {
        _getSteps.Response!.CustomerId.Should().Be(_customerId);
        _getSteps.Response!.PreferredMilkType.Should().Be("Oat");
        _getSteps.Response!.LikesExtraToppings.Should().BeTrue();
    }

    private async Task<CompositeStep> The_preference_update_response_should_contain_the_updated_values()
    {
        return Sub.Steps(
            _ => The_update_response_http_status_should_be_ok(),
            _ => The_update_response_should_be_valid_json(),
            _ => The_updated_preferences_should_have_the_new_milk_type(),
            _ => The_updated_preferences_should_have_the_new_favourite_item());
    }

    private async Task The_update_response_http_status_should_be_ok()
        => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_update_response_should_be_valid_json()
        => await _putSteps.ParseResponse();

    private async Task The_updated_preferences_should_have_the_new_milk_type()
        => _putSteps.Response!.PreferredMilkType.Should().Be("Almond");

    private async Task The_updated_preferences_should_have_the_new_favourite_item()
        => _putSteps.Response!.FavouriteItem.Should().Be("Belgian Waffles");

    private async Task The_preference_get_response_should_indicate_not_found()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    private async Task The_preference_response_should_indicate_bad_request()
        => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
