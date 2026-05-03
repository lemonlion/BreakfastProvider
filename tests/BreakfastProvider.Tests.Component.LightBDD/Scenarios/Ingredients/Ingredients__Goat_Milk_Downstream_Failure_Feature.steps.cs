using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

#pragma warning disable CS1998
public partial class Ingredients__Goat_Milk_Downstream_Failure_Feature : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;

    public Ingredients__Goat_Milk_Downstream_Failure_Feature()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    #region Given

    private async Task The_goat_service_will_return_service_unavailable()
        => _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.ServiceUnavailable);

    private async Task The_goat_service_will_return_an_invalid_response()
        => _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.InvalidResponse);

    #endregion

    #region When

    private async Task Goat_milk_is_requested()
        => await _goatMilkSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_goat_milk_response_should_indicate_a_bad_gateway()
    {
        return Sub.Steps(
            _ => The_goat_milk_response_http_status_should_be_bad_gateway(),
            _ => The_goat_milk_error_should_indicate_goat_service_unavailable());
    }

    private async Task The_goat_milk_response_http_status_should_be_bad_gateway()
        => Track.That(() => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));

    private async Task The_goat_milk_error_should_indicate_goat_service_unavailable()
    {
        var goatMilkErrorResponseBody = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => goatMilkErrorResponseBody.Should().Contain(DownstreamErrorMessages.GoatServiceUnavailableTitle));
    }

    #endregion
}
