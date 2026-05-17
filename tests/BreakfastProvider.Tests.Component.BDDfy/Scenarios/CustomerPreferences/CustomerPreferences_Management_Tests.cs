using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.CustomerPreferences;
using BreakfastProvider.Tests.Component.Shared.Models.CustomerPreferences;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.CustomerPreferences;

public class CustomerPreferences_Management_Tests : BaseFixture
{
    private readonly PutCustomerPreferenceSteps _putSteps;
    private readonly GetCustomerPreferenceSteps _getSteps;

    private string _customerId = null!;

    public CustomerPreferences_Management_Tests()
    {
        _putSteps = Get<PutCustomerPreferenceSteps>();
        _getSteps = Get<GetCustomerPreferenceSteps>();
    }

    [Fact]
    [HappyPath]
    public void Saving_customer_preferences_should_return_the_saved_preferences()
    {
        this.Given(x => x.A_valid_customer_preference_request_is_prepared())
            .When(x => x.The_customer_preferences_are_saved())
            .Then(x => x.The_response_should_contain_the_saved_preferences())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_existing_customer_preferences_should_return_the_preferences()
    {
        this.Given(x => x.Customer_preferences_exist())
            .When(x => x.The_customer_preferences_are_retrieved())
            .Then(x => x.The_response_should_contain_the_preferences())
            .BDDfy();
    }

    [Fact]
    public void Updating_customer_preferences_should_return_the_updated_preferences()
    {
        this.Given(x => x.Customer_preferences_exist())
            .When(x => x.The_customer_preferences_are_updated())
            .Then(x => x.The_response_should_contain_the_updated_values())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_non_existent_customer_preferences_should_return_not_found()
    {
        this.When(x => x.Non_existent_customer_preferences_are_retrieved())
            .Then(x => x.The_get_response_should_indicate_not_found())
            .BDDfy();
    }

    [Fact]
    public void Saving_customer_preferences_with_missing_customer_name_should_return_bad_request()
    {
        this.Given(x => x.A_customer_preference_request_with_missing_customer_name_is_prepared())
            .When(x => x.The_customer_preferences_are_saved())
            .Then(x => x.The_put_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_customer_preference_request_is_prepared()
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
        await Task.CompletedTask;
    }

    private async Task The_customer_preferences_are_saved()
    {
        await _putSteps.Send(_customerId);
    }

    private async Task The_response_should_contain_the_saved_preferences()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        _putSteps.Response!.PreferredMilkType.Should().Be("Oat");
        _putSteps.Response!.FavouriteItem.Should().Be("Blueberry Pancakes");
    }

    private async Task Customer_preferences_exist()
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
        await _putSteps.Send(_customerId);
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
    }

    private async Task The_customer_preferences_are_retrieved()
    {
        await _getSteps.RetrieveById(_customerId);
    }

    private async Task The_response_should_contain_the_preferences()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.CustomerId.Should().Be(_customerId);
        _getSteps.Response!.PreferredMilkType.Should().Be("Oat");
        _getSteps.Response!.LikesExtraToppings.Should().BeTrue();
    }

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

    private async Task The_response_should_contain_the_updated_values()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _putSteps.ParseResponse();
        _putSteps.Response!.PreferredMilkType.Should().Be("Almond");
        _putSteps.Response!.FavouriteItem.Should().Be("Belgian Waffles");
    }

    private async Task Non_existent_customer_preferences_are_retrieved()
    {
        await _getSteps.RetrieveById(Guid.NewGuid().ToString("N"));
    }

    private void The_get_response_should_indicate_not_found()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task A_customer_preference_request_with_missing_customer_name_is_prepared()
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
        await Task.CompletedTask;
    }

    private void The_put_response_should_indicate_bad_request()
    {
        _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
