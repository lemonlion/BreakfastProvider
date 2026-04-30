using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Staff;

[FeatureDescription($"/{Endpoints.Staff} - Managing kitchen staff members with full CRUD operations")]
public partial class Staff__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Adding_A_New_Staff_Member_Should_Return_The_Created_Member()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_staff_member_request(),
            when => The_staff_member_is_submitted(),
            then => The_staff_response_should_contain_the_created_member());
    }

    [Scenario]
    public async Task Retrieving_An_Existing_Staff_Member_Should_Return_The_Member()
    {
        await Runner.RunScenarioAsync(
            given => A_staff_member_exists(),
            when => The_staff_member_is_retrieved_by_id(),
            then => The_staff_get_response_should_contain_the_member());
    }

    [Scenario]
    public async Task Deleting_A_Staff_Member_Should_Return_No_Content()
    {
        await Runner.RunScenarioAsync(
            given => A_staff_member_exists(),
            when => The_staff_member_is_deleted(),
            then => The_staff_delete_response_should_indicate_no_content());
    }

    [Scenario]
    public async Task Adding_A_Staff_Member_With_An_Invalid_Role_Should_Return_A_Bad_Request_Response()
    {
        await Runner.RunScenarioAsync(
            given => A_staff_member_request_with_an_invalid_role(),
            when => The_staff_member_is_submitted(),
            then => The_staff_response_should_indicate_bad_request());
    }
}
