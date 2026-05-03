using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998
public partial class Specifications__Async_Api_Feature : BaseFixture
{
    private HttpResponseMessage? _asyncApiResponse;
    private string? _asyncApiJsonString;
    private JsonDocument? _asyncApiJson;
    
    #region Given
    #endregion

    #region When

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
                    return;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
            }

            if (attempt < maxRetries)
                await Task.Delay(500 * attempt);
        }
    }

    #endregion

    #region Then

    private async Task<CompositeStep> The_response_should_be_valid()
    {
        return Sub.Steps(
            _ => The_response_status_should_be_ok(),
            _ => The_response_should_be_valid_json());
    }

    private async Task The_response_status_should_be_ok()
    {
        Track.That(() => _asyncApiResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    private async Task The_response_should_be_valid_json()
    {
        var asyncApiResponseIsValidJson = _asyncApiJson is not null;
        Track.That(() => asyncApiResponseIsValidJson.Should().BeTrue(
            $"response body (first 500 chars): {_asyncApiJsonString?[..Math.Min(_asyncApiJsonString.Length, 500)]}"));
    }

    private async Task<CompositeStep> The_asyncapi_spec_is_written_to_disk()
    {
        return Sub.Steps(
            _ => The_asyncapi_spec_should_contain_NAME("asyncapi"),
            _ => The_asyncapi_spec_should_contain_NAME("info"),
            _ => The_asyncapi_spec_should_contain_NAME("defaultContentType"),
            _ => The_asyncapi_spec_should_contain_NAME("channels"),
            _ => The_asyncapi_spec_should_contain_NAME("operations"),
            _ => The_asyncapi_spec_should_contain_NAME("components"),
            _ => The_asyncapi_spec_is_written_to_disk_as_json());
    }

    private async Task The_asyncapi_spec_should_contain_NAME(string name)
        => Track.That(() => _asyncApiJson!.RootElement.GetProperty(name).Should().NotBeNull());

    private async Task The_asyncapi_spec_is_written_to_disk_as_json()
    {
        var path = $"{AsyncApiSpecs.SpecificationsFolderPath}{AsyncApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _asyncApiJsonString, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }

    #endregion
}
