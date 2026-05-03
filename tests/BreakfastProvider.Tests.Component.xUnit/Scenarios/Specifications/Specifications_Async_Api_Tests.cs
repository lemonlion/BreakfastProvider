using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Specifications;

public class Specifications_Async_Api_Tests : BaseFixture
{
    [Fact]
    [HappyPath]
    [Trait("Produces", "asyncapi.json")]
    public async Task The_AsyncApi_endpoint_should_return_a_valid_specification()
    {
        // When the asyncapi endpoint is called (with retries)
        HttpResponseMessage? asyncApiResponse = null;
        string? asyncApiJsonString = null;
        JsonDocument? asyncApiJson = null;

        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                asyncApiResponse = await Client.GetAsync(Endpoints.AsyncApi.AsyncApiSpec);
                asyncApiJsonString = await asyncApiResponse.Content.ReadAsStringAsync();
                if (Json.TryParse(asyncApiJsonString, out asyncApiJson))
                    break;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
            }

            if (attempt < maxRetries)
                await Task.Delay(500 * attempt);
        }

        // Then the response status should be ok
        Track.That(() => asyncApiResponse!.StatusCode.Should().Be(HttpStatusCode.OK));

        // And the response should be valid json
        var asyncApiResponseIsValidJson = asyncApiJson is not null;
        Track.That(() => asyncApiResponseIsValidJson.Should().BeTrue(
            $"response body (first 500 chars): {asyncApiJsonString?[..Math.Min(asyncApiJsonString.Length, 500)]}"));

        // And the asyncapi spec should contain expected top-level properties
        Track.That(() => asyncApiJson!.RootElement.GetProperty("asyncapi").Should().NotBeNull());
        Track.That(() => asyncApiJson!.RootElement.GetProperty("info").Should().NotBeNull());
        Track.That(() => asyncApiJson!.RootElement.GetProperty("defaultContentType").Should().NotBeNull());
        Track.That(() => asyncApiJson!.RootElement.GetProperty("channels").Should().NotBeNull());
        Track.That(() => asyncApiJson!.RootElement.GetProperty("operations").Should().NotBeNull());
        Track.That(() => asyncApiJson!.RootElement.GetProperty("components").Should().NotBeNull());

        // And the asyncapi spec is written to disk as json
        var path = $"{AsyncApiSpecs.SpecificationsFolderPath}{AsyncApiSpecs.JsonFileName}";
        const int writeRetries = 3;
        for (var attempt = 1; attempt <= writeRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, asyncApiJsonString, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < writeRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
