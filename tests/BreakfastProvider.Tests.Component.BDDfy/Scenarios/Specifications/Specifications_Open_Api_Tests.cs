using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Open_Api_Tests : BaseFixture
{
    private HttpResponseMessage? _swaggerResponse;
    private string? _swaggerJsonString;
    private JsonDocument? _swaggerJson;

    [Fact]
    [HappyPath]
    [Trait("Produces", "openapi.json")]
    public void The_OpenApi_endpoint_should_return_a_valid_specification()
    {
        this.When(x => x.The_open_api_endpoint_is_called())
            .Then(x => x.The_response_status_should_be_ok())
            .And(x => x.The_response_should_be_valid_json())
            .And(x => x.The_response_should_contain_all_the_endpoints())
            .And(x => x.The_openapi_spec_is_written_to_disk_as_json())
            .BDDfy();
    }

    #region Steps

    private async Task The_open_api_endpoint_is_called()
    {
        _swaggerResponse = await Client.GetAsync(Endpoints.Swagger.SwaggerJson);
        _swaggerJsonString = await _swaggerResponse.Content.ReadAsStringAsync();
    }

    private void The_response_status_should_be_ok()
    {
        _swaggerResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void The_response_should_be_valid_json()
    {
        var openApiResponseIsValidJson = Json.TryParse(_swaggerJsonString!, out _swaggerJson);
        openApiResponseIsValidJson.Should().BeTrue();
    }

    private void The_response_should_contain_all_the_endpoints()
    {
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull();
        _swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull();
    }

    private async Task The_openapi_spec_is_written_to_disk_as_json()
    {
        var path = $"{OpenApiSpecs.SpecificationsFolderPath}{OpenApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _swaggerJsonString, Encoding.UTF8);
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
