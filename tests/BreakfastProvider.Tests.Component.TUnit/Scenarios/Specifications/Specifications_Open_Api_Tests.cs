using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

public class Specifications_Open_Api_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    [Property("Produces", "openapi.json")]
    public async Task The_OpenApi_endpoint_should_return_a_valid_specification()
    {
        // When the open api endpoint is called
        var swaggerResponse = await Client.GetAsync(Endpoints.Swagger.SwaggerJson);

        // Then the response status should be ok
        await swaggerResponse.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);

        // And the response should be valid json
        var swaggerJsonString = await swaggerResponse.Content.ReadAsStringAsync();
        var openApiResponseIsValidJson = Json.TryParse(swaggerJsonString, out var swaggerJson);
        await openApiResponseIsValidJson.Should().BeTrue();

        // And the response should contain all the endpoints
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull();
        await swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull();

        // And the openapi spec is written to disk as json
        var path = $"{OpenApiSpecs.SpecificationsFolderPath}{OpenApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, swaggerJsonString, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
