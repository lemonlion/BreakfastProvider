using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.DailySpecials;

public class DailySpecials_Validation_Tests : BaseFixture
{
    [Theory]
    [InlineData("SpecialId", null, "Special ID is required", "'Special Id' is required.", "Bad Request")]
    [InlineData("Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public async Task Daily_special_order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        var validBase = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.DailySpecialsOrders, requests, new List<InvalidFieldFromRequest> { input });

        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        actual.ErrorMessage.Should().Be(expectedError);
        actual.ResponseStatus.Should().Be(expectedStatus);
        this.BDDfy();
    }
}
