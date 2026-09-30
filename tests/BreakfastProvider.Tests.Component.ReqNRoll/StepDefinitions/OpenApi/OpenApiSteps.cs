using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.OpenApi;

/// <summary>
/// Handles the OpenAPI, Scalar UI and AsyncAPI specification steps, and the "the response should be valid" step
/// every specification feature shares. Each When records what it fetched in the scenario's
/// <see cref="SpecificationDocumentContext"/>, which the shared step validates.
/// </summary>
[Binding]
public class ApiSpecificationSteps(SpecificationDocumentContext context, IReqnrollOutputHelper outputHelper)
{
    // ── OpenAPI When ──

    [When("the open api endpoint is called")]
    public async Task WhenTheOpenApiEndpointIsCalled()
    {
        await context.Retrieve(Endpoints.Swagger.SwaggerJson, SpecificationDocumentFormat.Json);
    }

    // ── Scalar UI When ──

    [When("the scalar ui endpoint is called")]
    public async Task WhenTheScalarUiEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.Swagger.ScalarUI);
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
                await context.Retrieve(Endpoints.AsyncApi.AsyncApiSpec, SpecificationDocumentFormat.Json);
                if (context.Document.Json is not null)
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
    public void ThenTheResponseShouldBeValid()
    {
        context.Format.Should().NotBeNull("the scenario should have fetched a specification document");
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

        switch (context.Format)
        {
            case SpecificationDocumentFormat.Json:
                var responseIsValidJson = context.Document.Json is not null;
                responseIsValidJson.Should().BeTrue(
                    $"response body (first 500 chars): {context.Document.Body?[..Math.Min(context.Document.Body.Length, 500)]}");
                break;
            case SpecificationDocumentFormat.DescriptorSet:
                context.Document.DescriptorSet.Should().NotBeNull();
                break;
        }
    }

    // ── OpenAPI Then ──

    [Then("the response should contain all the endpoints")]
    public void ThenTheResponseShouldContainAllTheEndpoints()
    {
        var paths = context.Document.Json!.RootElement.GetProperty("paths");
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
        var path = await context.Document.WriteToDocs(OpenApiSpecs.JsonFileName);
        outputHelper.AddAttachment(path);
    }

    // ── Scalar UI Then ──

    [Then("the response should be a valid scalar page")]
    public void ThenTheResponseShouldBeAValidScalarPage()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        var scalarUiResponseBody = context.Document.Body;
        scalarUiResponseBody.Should().Contain("<html");
        scalarUiResponseBody.Should().Contain("scalar");
    }

    // ── AsyncAPI Then ──

    [Then("the asyncapi spec is written to disk")]
    public async Task ThenTheAsyncapiSpecIsWrittenToDisk()
    {
        // Verify required sections
        var asyncApiJson = context.Document.Json!;
        asyncApiJson.RootElement.GetProperty("asyncapi").Should().NotBeNull();
        asyncApiJson.RootElement.GetProperty("info").Should().NotBeNull();
        asyncApiJson.RootElement.GetProperty("defaultContentType").Should().NotBeNull();
        asyncApiJson.RootElement.GetProperty("channels").Should().NotBeNull();
        asyncApiJson.RootElement.GetProperty("operations").Should().NotBeNull();
        asyncApiJson.RootElement.GetProperty("components").Should().NotBeNull();

        // Write to disk
        var path = await context.Document.WriteToDocs(AsyncApiSpecs.JsonFileName);
        outputHelper.AddAttachment(path);
    }
}
