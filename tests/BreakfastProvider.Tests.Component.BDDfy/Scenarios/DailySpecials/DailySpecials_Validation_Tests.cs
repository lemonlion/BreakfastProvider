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
    private string _expectedError = null!;
    private string _expectedStatus = null!;
    private VerifiableErrorResult? _actual;

    [Theory]
    [InlineData("SpecialId", null, "Special ID is required", "'Special Id' is required.", "Bad Request")]
    [InlineData("Quantity", "0", "Quantity must be greater than zero", "Quantity must be greater than zero.", "Bad Request")]
    public void Daily_special_order_with_invalid_field_should_return_bad_request(
        string field, string? value, string reason, string expectedError, string expectedStatus)
    {
        _input = new InvalidFieldFromRequest(field, value, reason);
        _expectedError = expectedError;
        _expectedStatus = expectedStatus;

        this.Given(x => x.A_valid_daily_special_order_request_with_an_invalid_field())
            .When(x => x.The_daily_special_order_request_is_sent())
            .Then(x => x.The_response_should_contain_the_expected_validation_error())
            .BDDfy();
    }

    #region Steps

    private Task A_valid_daily_special_order_request_with_an_invalid_field()
    {
        return Task.CompletedTask;
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

    private Task The_response_should_contain_the_expected_validation_error()
    {
        _actual!.ErrorMessage.Should().Be(_expectedError);
        _actual!.ResponseStatus.Should().Be(_expectedStatus);
        return Task.CompletedTask;
    }

    #endregion
}
