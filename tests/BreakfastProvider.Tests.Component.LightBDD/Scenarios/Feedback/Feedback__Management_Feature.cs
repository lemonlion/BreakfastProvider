using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Feedback;

[FeatureDescription($"/{Endpoints.Feedback} - Submitting and retrieving customer feedback")]
public partial class Feedback__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Submitting_Feedback_Should_Return_The_Created_Feedback()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_feedback_request(),
            when => The_feedback_is_submitted(),
            then => The_feedback_response_should_contain_the_created_feedback());
    }

    [Scenario]
    public async Task Retrieving_Existing_Feedback_By_Id_Should_Return_The_Feedback()
    {
        await Runner.RunScenarioAsync(
            given => A_feedback_entry_exists(),
            when => The_feedback_is_retrieved_by_id(),
            then => The_feedback_get_response_should_contain_the_feedback());
    }

    [Scenario]
    public async Task Listing_Feedback_For_An_Order_Should_Return_The_Feedback()
    {
        await Runner.RunScenarioAsync(
            given => A_feedback_entry_exists(),
            when => The_feedback_is_retrieved_by_order_id(),
            then => The_feedback_list_response_should_contain_the_feedback());
    }

    [Scenario]
    public async Task Retrieving_Non_Existent_Feedback_Should_Return_Not_Found()
    {
        await Runner.RunScenarioAsync(
            when => A_non_existent_feedback_is_retrieved(),
            then => The_feedback_get_response_should_indicate_not_found());
    }

    [Scenario]
    public async Task Submitting_Feedback_With_Missing_Customer_Name_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_feedback_request_with_missing_customer_name(),
            when => The_feedback_is_submitted(),
            then => The_feedback_response_should_indicate_bad_request());
    }

    [Scenario]
    public async Task Submitting_Feedback_With_Invalid_Rating_Should_Return_Bad_Request()
    {
        await Runner.RunScenarioAsync(
            given => A_feedback_request_with_an_invalid_rating(),
            when => The_feedback_is_submitted(),
            then => The_feedback_response_should_indicate_bad_request());
    }
}
