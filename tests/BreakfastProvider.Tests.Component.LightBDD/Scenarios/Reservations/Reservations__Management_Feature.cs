using BreakfastProvider.Tests.Component.Shared.Constants;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;
using TestTrackingDiagrams.LightBDD;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reservations;

[FeatureDescription($"/{Endpoints.Reservations} - Managing table reservations with full CRUD operations and cancellation")]
public partial class Reservations__Management_Feature
{
    [HappyPath]
    [Scenario]
    public async Task Creating_A_Reservation_Should_Return_The_Confirmed_Reservation()
    {
        await Runner.RunScenarioAsync(
            given => A_valid_reservation_request(),
            when => The_reservation_is_submitted(),
            then => The_reservation_response_should_contain_the_confirmed_booking());
    }

    [Scenario]
    public async Task Retrieving_An_Existing_Reservation_Should_Return_The_Reservation()
    {
        await Runner.RunScenarioAsync(
            given => A_reservation_exists(),
            when => The_reservation_is_retrieved_by_id(),
            then => The_reservation_get_response_should_contain_the_reservation());
    }

    [Scenario]
    public async Task Cancelling_A_Reservation_Should_Return_The_Cancelled_Reservation()
    {
        await Runner.RunScenarioAsync(
            given => A_reservation_exists(),
            when => The_reservation_is_cancelled(),
            then => The_cancellation_response_should_indicate_the_reservation_is_cancelled());
    }

    [Scenario]
    public async Task Cancelling_An_Already_Cancelled_Reservation_Should_Return_A_Conflict_Response()
    {
        await Runner.RunScenarioAsync(
            given => A_cancelled_reservation_exists(),
            when => The_reservation_is_cancelled_again(),
            then => The_cancellation_response_should_indicate_a_conflict());
    }

    [Scenario]
    public async Task Deleting_A_Reservation_Should_Return_No_Content()
    {
        await Runner.RunScenarioAsync(
            given => A_reservation_exists(),
            when => The_reservation_is_deleted(),
            then => The_reservation_delete_response_should_indicate_no_content());
    }
}
