using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

public class Toppings_Xss_Validation_Tests : BaseFixture
{
    [Theory]
    [InlineData("Name", "<script>alert('xss')</script>", "Script tag in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "<img src=x onerror=alert(1)>", "Event handler in name", "Name contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "<script>alert('xss')</script>", "Script tag in category", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Category", "javascript:alert(1)", "Javascript protocol", "Category contains potentially dangerous content.", "Bad Request")]
    [InlineData("Name", "", "Name is required", "'Name' is required.", "Bad Request")]
    [InlineData("Category", "", "Category is required", "'Category' is required.", "Bad Request")]
    public async Task Topping_request_with_invalid_or_dangerous_input_should_return_bad_request(
        string field, string value, string reason, string expectedError, string expectedStatus)
    {
        var validBase = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.Toppings, requests, new List<InvalidFieldFromRequest> { input });

        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
        this.BDDfy();
    }
}
