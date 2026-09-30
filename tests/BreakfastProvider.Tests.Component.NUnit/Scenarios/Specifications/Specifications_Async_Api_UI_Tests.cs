using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Specifications;

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
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.Body.Should().Contain("<html");
        _documentSteps.Body.Should().Contain(AsyncApiSpecs.UiRenderer);
        _documentSteps.Body.Should().Contain(Endpoints.AsyncApi.AsyncApiSpec);
    }
}
