using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Specifications;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Specifications;

public class Specifications_Async_Api_UI_Tests : BaseFixture
{
    private readonly SpecificationDocumentSteps _documentSteps;

    public Specifications_Async_Api_UI_Tests() => _documentSteps = Get<SpecificationDocumentSteps>();

    [Fact]
    [HappyPath]
    public void The_AsyncApi_UI_endpoint_should_return_a_valid_page()
    {
        this.When(x => x.The_asyncapi_ui_endpoint_is_called())
            .Then(x => x.The_response_should_be_a_valid_asyncapi_page())
            .BDDfy();
    }

    #region Steps

    private async Task The_asyncapi_ui_endpoint_is_called()
    {
        await _documentSteps.Retrieve(Endpoints.AsyncApi.AsyncApiUI);
    }

    private void The_response_should_be_a_valid_asyncapi_page()
    {
        _documentSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        _documentSteps.Body.Should().Contain("<html");
        _documentSteps.Body.Should().Contain(AsyncApiSpecs.UiRenderer);
        _documentSteps.Body.Should().Contain(Endpoints.AsyncApi.AsyncApiSpec);
    }

    #endregion
}
