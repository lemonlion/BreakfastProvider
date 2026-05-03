using System.Net;
using System.Text;
using System.Text.Json;
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
        const int maxRetries = 5;
        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                _asyncApiResponse = await appManager.Client.GetAsync(Endpoints.AsyncApi.AsyncApiSpec);
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

    // ── Shared Then ──

    [Then("the response should be valid")]
    public async Task ThenTheResponseShouldBeValid()
    {
        if (_swaggerResponse != null)
        {
            Track.That(() => _swaggerResponse.StatusCode.Should().Be(HttpStatusCode.OK));
            _swaggerJsonString = await _swaggerResponse.Content.ReadAsStringAsync();
            var openApiResponseIsValidJson = Json.TryParse(_swaggerJsonString, out _swaggerJson);
            Track.That(() => openApiResponseIsValidJson.Should().BeTrue());
        }
        else if (_asyncApiResponse != null)
        {
            Track.That(() => _asyncApiResponse.StatusCode.Should().Be(HttpStatusCode.OK));
            var asyncApiResponseIsValidJson = _asyncApiJson is not null;
            Track.That(() => asyncApiResponseIsValidJson.Should().BeTrue(
                $"response body (first 500 chars): {_asyncApiJsonString?[..Math.Min(_asyncApiJsonString.Length, 500)]}"));
        }
    }

    // ── OpenAPI Then ──

    [Then("the response should contain all the endpoints")]
    public void ThenTheResponseShouldContainAllTheEndpoints()
    {
        var paths = _swaggerJson!.RootElement.GetProperty("paths");
        Track.That(() => paths.GetProperty(Endpoints.Swagger.PancakesPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.WafflesPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.OrdersPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.OrderByIdPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.ToppingsPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.MenuPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.MilkPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.EggsPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.FlourPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.GoatMilkPath).Should().NotBeNull());
        Track.That(() => paths.GetProperty(Endpoints.Swagger.AuditLogsPath).Should().NotBeNull());
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
        Track.That(() => _scalarResponse!.StatusCode.Should().Be(HttpStatusCode.OK));
        _scalarHtml = await _scalarResponse.Content.ReadAsStringAsync();
        var scalarUiResponseBody = _scalarHtml;
        Track.That(() => scalarUiResponseBody.Should().Contain("<html"));
        Track.That(() => scalarUiResponseBody.Should().Contain("scalar"));
    }

    // ── AsyncAPI Then ──

    [Then("the asyncapi spec is written to disk")]
    public async Task ThenTheAsyncapiSpecIsWrittenToDisk()
    {
        // Verify required sections
        Track.That(() => _asyncApiJson!.RootElement.GetProperty("asyncapi").Should().NotBeNull());
        Track.That(() => _asyncApiJson.RootElement.GetProperty("info").Should().NotBeNull());
        Track.That(() => _asyncApiJson.RootElement.GetProperty("defaultContentType").Should().NotBeNull());
        Track.That(() => _asyncApiJson.RootElement.GetProperty("channels").Should().NotBeNull());
        Track.That(() => _asyncApiJson.RootElement.GetProperty("operations").Should().NotBeNull());
        Track.That(() => _asyncApiJson.RootElement.GetProperty("components").Should().NotBeNull());

        // Write to disk
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
}
