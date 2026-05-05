using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Downstream;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Ingredients__Goat_Milk_Sourcing_Feature : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;
    private readonly DownstreamRequestSteps _downstreamSteps;

    public Ingredients__Goat_Milk_Sourcing_Feature()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
        _downstreamSteps = Get<DownstreamRequestSteps>();
    }

    #region Given
    #endregion

    #region When

    private async Task Goat_milk_is_requested()
        => await _goatMilkSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_goat_milk_response_should_contain_fresh_goat_milk()
    {
        return Sub.Steps(
            _ => The_goat_milk_response_http_status_should_be_ok(),
            _ => The_goat_milk_response_should_be_valid_json(),
            _ => The_goat_milk_should_be_fresh());
    }

    private async Task The_goat_milk_response_http_status_should_be_ok()
        => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_goat_milk_response_should_be_valid_json()
        => _goatMilkSteps.GoatMilkResponse.Should().NotBeNull();

    private async Task The_goat_milk_should_be_fresh()
        => _goatMilkSteps.GoatMilkResponse.GoatMilk.Should().Be(GoatServiceDefaults.FreshGoatMilk);

    [SkipStepIf(nameof(Settings.RunAgainstExternalServiceUnderTest), DownstreamFakeRequestStoreIsUnavailableInPostDeploymentEnvironments)]
    private async Task The_goat_service_should_have_received_a_goat_milk_request()
        => _downstreamSteps.AssertGoatServiceReceivedGoatMilkRequest();

    #endregion
}
