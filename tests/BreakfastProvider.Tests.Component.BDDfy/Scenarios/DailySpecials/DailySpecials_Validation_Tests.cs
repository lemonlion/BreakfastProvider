using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.DailySpecials;

public class DailySpecials_Validation_Tests : BaseFixture
{
    private InvalidFieldFromRequest _input = null!;
    private VerifiableErrorResult? _actual;

    [Theory]
    [InlineData("SpecialId", null, "Special ID is required", "'Special Id' is required.", "Bad Request")]
    [InlineData("Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public void Daily_special_order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        this.Given(x => x.A_daily_special_order_request_with_an_invalid_FIELD(field, value, reason))
            .When(x => x.The_daily_special_order_request_is_sent())
            .Then(x => x.The_response_should_contain_error(expectedError))
            .And(x => x.The_response_status_should_be(expectedStatus))
            .BDDfy();
    }

    #region Steps

    private void A_daily_special_order_request_with_an_invalid_FIELD(string field, string? value, string reason)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
    }

    private async Task The_daily_special_order_request_is_sent()
    {
        var validBase = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        var requests = ValidationHelper.CreateValidationRequests(validBase, [_input]);
        var responses = await ValidationHelper.SendValidationRequests(
            Client, RequestId, Endpoints.DailySpecialsOrders, requests, [_input]);
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
