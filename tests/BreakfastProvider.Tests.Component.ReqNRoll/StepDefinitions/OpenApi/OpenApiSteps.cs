using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Util;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.OpenApi;

/// <summary>
/// Handles OpenAPI, Scalar UI, and AsyncAPI specification steps.
/// Combined into one binding class because "the response should be valid" is shared across features.
/// </summary>
[Binding]
public class ApiSpecificationSteps(AppManager appManager)
{
    private HttpResponseMessage? _swaggerResponse;
    private string? _swaggerJsonString;
    private JsonDocument? _swaggerJson;
    private HttpResponseMessage? _scalarResponse;
    private string? _scalarHtml;
    private HttpResponseMessage? _asyncApiResponse;
    private string? _asyncApiJsonString;
    private string? _asyncApiJsonStringToPublish;
    private JsonDocument? _asyncApiJson;

    // ── OpenAPI When ──

    [When("the open api endpoint is called")]
    public async Task WhenTheOpenApiEndpointIsCalled()
    {
        _swaggerResponse = await appManager.Client.GetAsync(Endpoints.Swagger.SwaggerJson);
    }

    // ── Scalar UI When ──

    [When("the scalar ui endpoint is called")]
    public async Task WhenTheScalarUiEndpointIsCalled()
    {
        _scalarResponse = await appManager.Client.GetAsync(Endpoints.Swagger.ScalarUI);
    }

    // ── AsyncAPI When ──

    [When("the asyncapi endpoint is called")]
    public async Task WhenTheAsyncapiEndpointIsCalled()
    {
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _asyncApiResponse = await appManager.Client.GetAsync(Endpoints.AsyncApi.AsyncApiSpec);
                return;
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await Task.Delay(200 * attempt);
            }
        }
    }

    // ── Shared Then ──

    [Then("the response should be valid")]
    public async Task ThenTheResponseShouldBeValid()
    {
        if (_swaggerResponse != null)
        {
            _swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            _swaggerJsonString = await _swaggerResponse.Content.ReadAsStringAsync();
            Json.TryParse(_swaggerJsonString, out _swaggerJson).Should().BeTrue();
        }
        else if (_asyncApiResponse != null)
        {
            _asyncApiResponse.StatusCode.Should().Be(HttpStatusCode.OK);
            _asyncApiJsonString = await _asyncApiResponse.Content.ReadAsStringAsync();
            Json.TryParse(_asyncApiJsonString, out _asyncApiJson).Should().BeTrue();
        }
    }

    // ── OpenAPI Then ──

    [Then("the response should contain all the endpoints")]
    public void ThenTheResponseShouldContainAllTheEndpoints()
    {
        var paths = _swaggerJson!.RootElement.GetProperty("paths");
        paths.GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull();
        paths.GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull();
    }

    [Then("the openapi spec is written to disk")]
    public async Task ThenTheOpenapiSpecIsWrittenToDisk()
    {
        var path = $"{OpenApiSpecs.SpecificationsFolderPath}{OpenApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _swaggerJsonString!, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }

    // ── Scalar UI Then ──

    [Then("the response should be a valid scalar page")]
    public async Task ThenTheResponseShouldBeAValidScalarPage()
    {
        _scalarResponse!.StatusCode.Should().Be(HttpStatusCode.OK);
        _scalarHtml = await _scalarResponse.Content.ReadAsStringAsync();
        _scalarHtml.Should().Contain("<html");
        _scalarHtml.Should().Contain("scalar");
    }

    // ── AsyncAPI Then ──

    [Then("the asyncapi spec is written to disk")]
    public async Task ThenTheAsyncapiSpecIsWrittenToDisk()
    {
        // Add x-pub-settings section
        var openApiDocument = (JsonObject)JsonNode.Parse(_asyncApiJsonString!)!;
        var serializationOptions = new JsonSerializerOptions(Json.SerializerOptions)
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        openApiDocument.Add("x-pub-settings", new JsonObject
        {
            { "pub-ready", true },
            { "tags", new JsonArray { "Breakfast" } },
            { "team", "Griddle" }
        });

        _asyncApiJsonStringToPublish = JsonSerializer.Serialize(openApiDocument, serializationOptions);
        _asyncApiJson = JsonDocument.Parse(_asyncApiJsonStringToPublish);

        // Verify required sections
        _asyncApiJson.RootElement.GetProperty("asyncapi").Should().NotBeNull();
        _asyncApiJson.RootElement.GetProperty("info").Should().NotBeNull();
        _asyncApiJson.RootElement.GetProperty("defaultContentType").Should().NotBeNull();
        _asyncApiJson.RootElement.GetProperty("channels").Should().NotBeNull();
        _asyncApiJson.RootElement.GetProperty("operations").Should().NotBeNull();
        _asyncApiJson.RootElement.GetProperty("components").Should().NotBeNull();

        // Verify x-pub-settings
        var xPubSettings = _asyncApiJson.RootElement.GetProperty("x-pub-settings");
        xPubSettings.GetProperty("pub-ready").GetBoolean().Should().BeTrue();
        xPubSettings.GetProperty("team").GetString().Should().Be("Griddle");
        xPubSettings.GetProperty("tags").EnumerateArray().Should().OnlyContain(x => x.GetString() == "Breakfast");

        // Write to disk
        var path = $"{AsyncApiSpecs.SpecificationsFolderPath}{AsyncApiSpecs.JsonFileName}";
        const int maxRetries = 3;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                await File.WriteAllTextAsync(path, _asyncApiJsonStringToPublish, Encoding.UTF8);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await Task.Delay(500 * attempt);
            }
        }
    }
}
