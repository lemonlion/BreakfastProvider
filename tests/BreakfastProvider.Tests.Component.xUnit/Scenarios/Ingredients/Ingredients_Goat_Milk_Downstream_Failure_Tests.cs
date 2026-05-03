using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Ingredients;

public class Ingredients_Goat_Milk_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetGoatMilkSteps _goatMilkSteps;

    public Ingredients_Goat_Milk_Downstream_Failure_Tests()
    {
        _goatMilkSteps = Get<GetGoatMilkSteps>();
    }

    [Fact]
    public async Task Requesting_goat_milk_when_goat_service_unavailable_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the goat service will return service unavailable
        _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.ServiceUnavailable);

        // When goat milk is requested
        await _goatMilkSteps.Retrieve();

        // Then the goat milk response should indicate a bad gateway
        Track.That(() => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));
        var goatMilkErrorResponseBody = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => goatMilkErrorResponseBody.Should().Contain(DownstreamErrorMessages.GoatServiceUnavailableTitle));
    }

    [Fact]
    public async Task Requesting_goat_milk_when_goat_service_returns_invalid_response_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the goat service will return an invalid response
        _goatMilkSteps.AddHeader(FakeScenarioHeaders.GoatService, FakeScenarios.InvalidResponse);

        // When goat milk is requested
        await _goatMilkSteps.Retrieve();

        // Then the goat milk response should indicate a bad gateway
        Track.That(() => _goatMilkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));
        var goatMilkErrorResponseBody = await _goatMilkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => goatMilkErrorResponseBody.Should().Contain(DownstreamErrorMessages.GoatServiceUnavailableTitle));
    }
}
