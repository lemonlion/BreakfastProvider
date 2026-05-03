using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Specifications;

public class Specifications_Open_Api_Tests : BaseFixture
{
    [Fact]
    [HappyPath]
    [Trait("Produces", "openapi.json")]
    public async Task The_OpenApi_endpoint_should_return_a_valid_specification()
    {
        // When the open api endpoint is called
        var swaggerResponse = await Client.GetAsync(Endpoints.Swagger.SwaggerJson);

        // Then the response status should be ok
        Track.That(() => swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK));

        // And the response should be valid json
        var swaggerJsonString = await swaggerResponse.Content.ReadAsStringAsync();
        var openApiResponseIsValidJson = Json.TryParse(swaggerJsonString, out var swaggerJson);
        Track.That(() => openApiResponseIsValidJson.Should().BeTrue());

        // And the response should contain all the endpoints
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull());
        Track.That(() => swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull());

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
