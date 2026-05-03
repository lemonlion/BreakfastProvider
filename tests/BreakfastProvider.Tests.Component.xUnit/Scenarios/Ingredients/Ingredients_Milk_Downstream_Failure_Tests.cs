using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Ingredients;

public class Ingredients_Milk_Downstream_Failure_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;

    public Ingredients_Milk_Downstream_Failure_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
    }

    [Fact]
    public async Task Requesting_milk_when_cow_service_unavailable_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the cow service will return service unavailable
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.ServiceUnavailable);

        // When milk is requested
        await _milkSteps.Retrieve();

        // Then the milk response should indicate a bad gateway
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));
        var milkErrorResponseBody = await _milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => milkErrorResponseBody.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle));
    }

    [Fact]
    public async Task Requesting_milk_when_cow_service_times_out_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the cow service will return a timeout
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.Timeout);

        // When milk is requested
        await _milkSteps.Retrieve();

        // Then the milk response should indicate a bad gateway
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));
        var milkErrorResponseBody = await _milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => milkErrorResponseBody.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle));
    }

    [Fact]
    public async Task Requesting_milk_when_cow_service_returns_invalid_response_should_return_bad_gateway()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the cow service will return an invalid response
        _milkSteps.AddHeader(FakeScenarioHeaders.CowService, FakeScenarios.InvalidResponse);

        // When milk is requested
        await _milkSteps.Retrieve();

        // Then the milk response should indicate a bad gateway
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadGateway));
        var milkErrorResponseBody = await _milkSteps.ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => milkErrorResponseBody.Should().Contain(DownstreamErrorMessages.CowServiceUnavailableTitle));
    }
}
