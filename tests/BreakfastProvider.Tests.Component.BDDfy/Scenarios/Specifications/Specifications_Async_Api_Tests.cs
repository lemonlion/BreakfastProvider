using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;
using TestTrackingDiagrams.Tracking;
using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Async_Api_Tests : BaseFixture
{
    private HttpResponseMessage? _asyncApiResponse;
    private string? _asyncApiJsonString;
    private JsonDocument? _asyncApiJson;

    [Fact]
    [HappyPath]
    [Trait("Produces", "asyncapi.json")]
    public void The_AsyncApi_endpoint_should_return_a_valid_specification()
    {
        this.When(x => x.The_asyncapi_endpoint_is_called())
            .Then(x => x.The_response_status_should_be_ok())
            .And(x => x.The_response_should_be_valid_json())
            .And(x => x.The_asyncapi_spec_should_contain_expected_top_level_properties())
            .And(x => x.The_asyncapi_spec_is_written_to_disk_as_json())
            .BDDfy();
    }

    #region Steps

    private async Task The_asyncapi_endpoint_is_called()
    {
        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _asyncApiResponse = await Client.GetAsync(Endpoints.AsyncApi.AsyncApiSpec);
                _asyncApiJsonString = await _asyncApiResponse.Content.ReadAsStringAsync();
                if (Json.TryParse(_asyncApiJsonString, out _asyncApiJson))
                    break;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
            }

            if (attempt < maxRetries)
                await Task.Delay(500 * attempt);
        }
    }

    private void The_response_status_should_be_ok()
    {
        _asyncApiResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void The_response_should_be_valid_json()
    {
        var asyncApiResponseIsValidJson = _asyncApiJson is not null;
        asyncApiResponseIsValidJson.Should().BeTrue(
            $"response body (first 500 chars): {_asyncApiJsonString?[..Math.Min(_asyncApiJsonString.Length, 500)]}");
    }

    private void The_asyncapi_spec_should_contain_expected_top_level_properties()
    {
        _asyncApiJson!.RootElement.GetProperty("asyncapi").Should().NotBeNull();
        _asyncApiJson!.RootElement.GetProperty("info").Should().NotBeNull();
        _asyncApiJson!.RootElement.GetProperty("defaultContentType").Should().NotBeNull();
        _asyncApiJson!.RootElement.GetProperty("channels").Should().NotBeNull();
        _asyncApiJson!.RootElement.GetProperty("operations").Should().NotBeNull();
        _asyncApiJson!.RootElement.GetProperty("components").Should().NotBeNull();
    }

    private async Task The_asyncapi_spec_is_written_to_disk_as_json()
    {
        var path = $"{AsyncApiSpecs.SpecificationsFolderPath}{AsyncApiSpecs.JsonFileName}";
        const int writeRetries = 3;
        for (var attempt = 1; attempt <= writeRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _asyncApiJsonString, Encoding.UTF8);
                Track.Attachment(path, "asyncapi.json");
                return;
            }
            catch (IOException) when (attempt < writeRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }

    #endregion
}
