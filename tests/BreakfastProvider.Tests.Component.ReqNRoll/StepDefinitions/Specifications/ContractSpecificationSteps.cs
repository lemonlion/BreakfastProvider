using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Specifications;

/// <summary>
/// The contract pages and documents beyond OpenAPI: the AsyncAPI UI. What a When fetches goes into the scenario's
/// <see cref="SpecificationDocumentContext"/>.
/// </summary>
[Binding]
public class ContractSpecificationSteps(SpecificationDocumentContext context)
{
    // ── AsyncAPI UI ──

    [When("the asyncapi ui endpoint is called")]
    public async Task WhenTheAsyncapiUiEndpointIsCalled()
    {
        await context.Document.Retrieve(Endpoints.AsyncApi.AsyncApiUI);
    }

    [Then("the response should be a valid asyncapi page")]
    public void ThenTheResponseShouldBeAValidAsyncapiPage()
    {
        context.Document.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        context.Document.Body.Should().Contain("<html");
        context.Document.Body.Should().Contain(AsyncApiSpecs.UiRenderer);
        context.Document.Body.Should().Contain(Endpoints.AsyncApi.AsyncApiSpec);
    }
}
