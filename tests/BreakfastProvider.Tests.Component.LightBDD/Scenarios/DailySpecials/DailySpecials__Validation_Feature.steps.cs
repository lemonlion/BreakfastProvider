using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework.Parameters;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

#pragma warning disable CS1998
public partial class DailySpecials__Validation_Feature : BaseFixture
{
    private readonly List<TestDailySpecialOrderRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    #region Given

    private async Task Valid_daily_special_order_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        _validationInputs.AddRange(inputs);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    #endregion

    #region When

    private async Task The_invalid_daily_special_order_requests_are_submitted()
        => _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(Client, RequestId, Endpoints.DailySpecialsOrders, _validationRequests, _validationInputs,
                onTestDelimiter: TrackingDiagramOverride.InsertTestDelimiter));

    #endregion

    #region Then

    private async Task The_responses_should_each_contain_the_validation_error_for_the_invalid_field(
        VerifiableDataTable<VerifiableErrorResult> expectedOutputs)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        expectedOutputs.SetActual(actualResults);
    }

    #endregion
}
