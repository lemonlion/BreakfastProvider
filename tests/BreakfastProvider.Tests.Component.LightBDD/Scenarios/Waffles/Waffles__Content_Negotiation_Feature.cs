using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Waffles;

[FeatureDescription($"/{Endpoints.Waffles} - Content negotiation and unsupported media types")]
public partial class Waffles__Content_Negotiation_Feature
{
    [Scenario]
    [InlineData("text/plain")]
    [InlineData("application/xml")]
    [InlineData("text/html")]
    public async Task Sending_A_Request_With_An_Unsupported_Content_Type_Should_Return_An_Unsupported_Media_Type_Response(string contentType)
    {
        await Runner.RunScenarioAsync(
            given => A_waffle_request_with_content_type(contentType),
            when => The_waffles_are_prepared(),
            then => The_response_should_indicate_unsupported_media_type());
    }
}
