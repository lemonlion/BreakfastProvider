using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Common.Validation;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using BreakfastProvider.Tests.Component.Shared.Models.Validation;
using LightBDD.Framework;
using LightBDD.Framework.Parameters;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

#pragma warning disable CS1998
public partial class Toppings__Update_Feature : BaseFixture
{
    private readonly PutToppingSteps _putSteps;
    private Guid _toppingId;

    // Well-known topping ID from the static list in ToppingsController
    private static readonly Guid KnownBlueberryToppingId = ToppingDefaults.KnownBlueberryToppingId;

    private readonly List<TestUpdateToppingRequest> _validationRequests = [];
    private readonly List<HttpResponseMessage> _validationResponses = [];
    private readonly List<InvalidFieldFromRequest> _validationInputs = [];

    public Toppings__Update_Feature()
    {
        _putSteps = Get<PutToppingSteps>();
    }

    #region Given

    private async Task A_known_topping_exists()
    {
        _toppingId = KnownBlueberryToppingId;
    }

    private async Task A_topping_id_that_does_not_exist()
    {
        _toppingId = Guid.NewGuid();
    }

    private async Task A_valid_update_topping_request()
    {
        _putSteps.Request = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    private async Task Valid_update_topping_requests_with_an_invalid_field(InputTable<InvalidFieldFromRequest> inputs)
    {
        var validBase = new TestUpdateToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        _validationInputs.AddRange(inputs);
        _validationRequests.AddRange(ValidationHelper.CreateValidationRequests(validBase, inputs));
    }

    #endregion

    #region When

    private async Task The_topping_is_updated()
        => await _putSteps.Send(_toppingId);

    private async Task The_invalid_update_topping_requests_are_submitted()
        => _validationResponses.AddRange(
            await ValidationHelper.SendPutValidationRequests(
                Client, RequestId, $"{Endpoints.Toppings}/{_toppingId}", _validationRequests, _validationInputs));

    #endregion

    #region Then

    private async Task<CompositeStep> The_update_response_should_contain_the_updated_topping()
    {
        return Sub.Steps(
            _ => The_update_response_http_status_should_be_ok(),
            _ => The_update_response_should_be_valid_json(),
            _ => The_updated_topping_should_have_the_correct_id(),
            _ => The_updated_topping_should_have_the_correct_name(),
            _ => The_updated_topping_should_have_the_correct_category());
    }

    private async Task The_update_response_http_status_should_be_ok()
        => Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_update_response_should_be_valid_json()
        => await _putSteps.ParseResponse();

    private async Task The_updated_topping_should_have_the_correct_id()
        => Track.That(() => _putSteps.Response!.ToppingId.Should().Be(KnownBlueberryToppingId));

    private async Task The_updated_topping_should_have_the_correct_name()
        => Track.That(() => _putSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries));

    private async Task The_updated_topping_should_have_the_correct_category()
        => Track.That(() => _putSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory));

    private async Task The_update_response_should_indicate_not_found()
        => Track.That(() => _putSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));

    private async Task The_update_responses_should_each_contain_the_validation_error_for_the_invalid_field(
        VerifiableDataTable<VerifiableErrorResult> expectedOutputs)
    {
        var actualResults = await ValidationHelper.ParseValidationResponses(_validationResponses);
        expectedOutputs.SetActual(actualResults);
    }

    #endregion
}
