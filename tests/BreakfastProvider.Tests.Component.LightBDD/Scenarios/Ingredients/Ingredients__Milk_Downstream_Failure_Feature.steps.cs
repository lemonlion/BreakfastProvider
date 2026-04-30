using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Ingredients;

#pragma warning disable CS1998
public partial class Ingredients__Milk_Downstream_Failure_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;

    public Ingredients__Milk_Downstream_Failure_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
    }

    #region Given

    private async Task The_cow_service_will_return_service_unavailable()
        => _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.ServiceUnavailable);

    private async Task The_cow_service_will_return_a_timeout()
        => _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.Timeout);

    private async Task The_cow_service_will_return_an_invalid_response()
        => _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.InvalidResponse);

    #endregion

    #region When

    private async Task Milk_is_requested()
        => await _milkSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_milk_response_should_indicate_a_bad_gateway()
    {
        return Sub.Steps(
            _ => The_milk_response_http_status_should_be_bad_gateway(),
            _ => The_milk_error_should_indicate_cow_service_unavailable());
    }

    private async Task The_milk_response_http_status_should_be_bad_gateway()
        => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway);

    private async Task The_milk_error_should_indicate_cow_service_unavailable()
    {
        var content = await _milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        content.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle);
    }

    #endregion
}
