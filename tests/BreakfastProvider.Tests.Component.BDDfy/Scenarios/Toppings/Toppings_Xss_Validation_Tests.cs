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
        this.Given(x => x.A_topping_request_with_an_invalid_FIELD(field, value, reason))
            .When(x => x.The_topping_request_is_sent())
            .Then(x => x.The_response_should_contain_error(expectedError))
            .And(x => x.The_response_status_should_be(expectedStatus))
            .BDDfy();
    }

    #region Steps

    private void A_topping_request_with_an_invalid_FIELD(string field, string value, string reason)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
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

    private void The_response_should_contain_error(string expectedError)
    {
        _actual!.ErrorMessage.Should().Be(expectedError);
    }

    private void The_response_status_should_be(string expectedStatus)
    {
        _actual!.ResponseStatus.Should().Be(expectedStatus);
    }

    #endregion
}
