using System.Net;
using System.Text;
using System.Text.Json;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.Tracking;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Specifications;

public class Specifications_Open_Api_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    [Category("Produces: openapi.json")]
    public async Task The_OpenApi_endpoint_should_return_a_valid_specification()
    {
        // When the open api endpoint is called
        var swaggerResponse = await Client.GetAsync(Endpoints.Swagger.SwaggerJson);

        // Then the response status should be ok
        swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // And the response should be valid json
        var swaggerJsonString = await swaggerResponse.Content.ReadAsStringAsync();
        var openApiResponseIsValidJson = Json.TryParse(swaggerJsonString, out var swaggerJson);
        openApiResponseIsValidJson.Should().BeTrue();

        // And the response should contain all the endpoints
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull();
        swaggerJson!.RootElement.GetProperty("paths").GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull();

        // And the openapi spec is written to disk as json
        var path = $"{OpenApiSpecs.SpecificationsFolderPath}{OpenApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, swaggerJsonString, Encoding.UTF8);
                Track.Attachment(path, "openapi.json");
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
