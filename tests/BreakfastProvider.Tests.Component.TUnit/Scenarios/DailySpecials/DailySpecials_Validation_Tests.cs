using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.DailySpecials;

public class DailySpecials_Validation_Tests : BaseFixture
{
    [Test]
    [Arguments("SpecialId", null, "Special ID is required", "'Special Id' is required.", "Bad Request")]
    [Arguments("Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public async Task Daily_special_order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        // Given valid daily special order requests with an invalid field
        var validBase = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        var input = new InvalidFieldFromRequest(field, value, reason);
        var requests = ValidationHelper.CreateValidationRequests(validBase, new List<InvalidFieldFromRequest> { input });

        // When the invalid daily special order requests are submitted
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.DailySpecialsOrders, requests, new List<InvalidFieldFromRequest> { input });

        // Then the responses should contain the validation error for the invalid field
        var actualResults = await ValidationHelper.ParseValidationResponses(responses);
        var actual = actualResults.Single();
        await actual.ErrorMessage.Should().BeEqualTo(expectedError);
        await actual.ResponseStatus.Should().BeEqualTo(expectedStatus);
    }
}
