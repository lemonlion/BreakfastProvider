using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

public class Toppings_Xss_Validation_Tests : BaseFixture
{
    private InvalidFieldFromRequest _input = null!;
    private string _expectedError = null!;
    private string _expectedStatus = null!;
    private VerifiableErrorResult? _actual;

    [Theory]
    [InlineData("Name", "<script>alert('xss')</script>", "Script tag in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "<img src=x onerror=alert(1)>", "Event handler in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "<script>alert('xss')</script>", "Script tag in category", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "javascript:alert(1)", "Javascript protocol", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "", "Name is required", "'Name' is required.", "Bad Request")]
    [InlineData("Category", "", "Category is required", "'Category' is required.", "Bad Request")]
    public void Topping_request_with_invalid_or_dangerous_input_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
        _expectedError = expectedError;
        _expectedStatus = expectedStatus;

        this.Given(x => x.A_valid_topping_request_with_an_invalid_field())
            .When(x => x.The_topping_request_is_sent())
            .Then(x => x.The_response_should_contain_the_expected_validation_error())
            .BDDfy();
    }

    #region Steps

    private Task A_valid_topping_request_with_an_invalid_field()
    {
        return Task.CompletedTask;
    }

    private async Task The_topping_request_is_sent()
    {
        var validBase = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Toppings, requests, [_input]);
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        _actual = actualResults.Single();
    }

    private Task The_response_should_contain_the_expected_validation_error()
    {
        _actual!.ErrorMessage.Should().Be(_expectedError);
        _actual!.ResponseStatus.Should().Be(_expectedStatus);
        return Task.CompletedTask;
    }

    #endregion
}
