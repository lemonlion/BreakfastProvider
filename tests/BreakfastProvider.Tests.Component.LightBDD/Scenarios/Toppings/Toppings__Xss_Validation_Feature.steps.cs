using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework.Parameters;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

#pragma warning disable CS1998
public partial class Toppings__Xss_Validation_Feature : BaseFixture
{
    private readonly List<TestToppingRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    #region Given

    private async Task Valid_topping_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        _validationInputs.AddRange(inputs);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    #endregion

    #region When

    private async Task The_invalid_topping_requests_are_submitted()
        => _validationResponses.AddRange(
            await ValidationHelper.SendValidationRequests(Client, RequestId, Endpoints.Toppings, _validationRequests, _validationInputs,
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
