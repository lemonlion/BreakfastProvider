using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Specifications;

#pragma warning disable CS1998
public partial class Specifications__Open_Api_Feature : BaseFixture
{
    private HttpResponseMessage? _swaggerResponse;
    private string? _swaggerJsonString;
    private JsonDocument? _swaggerJson;

    #region Given
    #endregion

    #region When

    private async Task The_open_api_endpoint_is_called()
    {
        _swaggerResponse = await Client.GetAsync(Endpoints.Swagger.SwaggerJson);
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
        Track.That(() => _swaggerResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
    }

    private async Task The_response_should_be_valid_json()
    {
        _swaggerJsonString = await _swaggerResponse!.Content.ReadAsStringAsync();
        var openApiResponseIsValidJson = Json.TryParse(_swaggerJsonString, out _swaggerJson);
        Track.That(() => openApiResponseIsValidJson.Should().BeTrue());
    }

    private async Task<CompositeStep> The_response_should_contain_all_the_endpoints()
    {
        return Sub.Steps(
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.PancakesPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.WafflesPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.OrdersPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.OrderByIdPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.ToppingsPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.MenuPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.MilkPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.EggsPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.FlourPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.GoatMilkPath),
            _ => The_response_should_contain_the_endpoint_PATH(Endpoints.Swagger.AuditLogsPath));
    }

    private async Task The_response_should_contain_the_endpoint_PATH(string path)
    {
        Track.That(() => _swaggerJson!.RootElement.GetProperty("paths").GetProperty(path).Should().NotBeNull());
    }
    private async Task<CompositeStep> The_openapi_spec_is_written_to_disk()
    {
        return Sub.Steps(
            _ => The_openapi_spec_is_written_to_disk_as_json());
    }

    private async Task The_openapi_spec_is_written_to_disk_as_json()
    {
        var path = $"{OpenApiSpecs.SpecificationsFolderPath}{OpenApiSpecs.JsonFileName}";
        var content = _swaggerJsonString!;
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, content, Encoding.UTF8);
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
