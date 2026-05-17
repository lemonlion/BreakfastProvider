using System.Net;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.NUnit.Infrastructure;
using Kronikol.NUnit4;

namespace BreakfastProvider.Tests.Component.NUnit.Scenarios.Specifications;

public class Specifications_Open_Api_Scalar_UI_Tests : BaseFixture
{
    [Test]
    [HappyPath]
    public async Task The_Scalar_UI_endpoint_should_return_a_valid_page()
    {
        // When the scalar ui endpoint is called
        var scalarResponse = await Client.GetAsync(Endpoints.Swagger.ScalarUI);

        // Then the response status should be ok
        scalarResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // And the response should be valid html
        var scalarUiResponseBody = await scalarResponse.Content.ReadAsStringAsync();
        scalarUiResponseBody.Should().Contain("<html");

        // And the response should refer to scalar
        scalarUiResponseBody.Should().Contain("scalar");
    }
}
