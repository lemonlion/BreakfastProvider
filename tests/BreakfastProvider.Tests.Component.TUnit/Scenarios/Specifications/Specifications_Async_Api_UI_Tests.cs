using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Specifications;

public class Specifications_Async_Api_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Async_Api_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Test]
    [HappyPath]
    public async Task The_AsyncApi_UI_endpoint_should_return_a_valid_page()
    {
        // When the asyncapi ui endpoint is called
        await _documentSteps.Retrieve(Endpoints.AsyncApi.AsyncApiUI);

        // Then the response should be a valid asyncapi page
        await _documentSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _documentSteps.Body!.Should().Contain("<html");
        await _documentSteps.Body!.Should().Contain(AsyncApiSpecs.UiRenderer);
        await _documentSteps.Body!.Should().Contain(Endpoints.AsyncApi.AsyncApiSpec);
    }
}
