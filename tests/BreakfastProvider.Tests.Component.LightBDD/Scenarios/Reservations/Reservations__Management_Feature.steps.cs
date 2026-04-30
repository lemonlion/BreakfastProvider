using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reservations;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reservations__Management_Feature : BaseFixture
{
    private readonly PostReservationSteps _postSteps;
    private readonly GetReservationSteps _getSteps;
    private readonly CancelReservationSteps _cancelSteps;
    private int _createdReservationId;

    public Reservations__Management_Feature()
    {
        _postSteps = Get<PostReservationSteps>();
        _getSteps = Get<GetReservationSteps>();
        _cancelSteps = Get<CancelReservationSteps>();
    }

    #region Given

    private async Task A_valid_reservation_request()
    {
        _postSteps.Request = new TestReservationRequest
        {
            CustomerName = $"Customer-{Guid.NewGuid():N}",
            TableNumber = Random.Shared.Next(1, 50),
            PartySize = Random.Shared.Next(1, 10),
            ReservedAt = DateTime.UtcNow.AddHours(2),
            ContactPhone = "07700900000"
        };
    }

    private async Task<CompositeStep> A_reservation_exists()
    {
        return Sub.Steps(
            _ => A_valid_reservation_request(),
            _ => The_reservation_is_submitted(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReservationId = _postSteps.Response!.Id;
    }

    private async Task<CompositeStep> A_cancelled_reservation_exists()
    {
        return Sub.Steps(
            _ => A_valid_reservation_request(),
            _ => The_reservation_is_submitted(),
            _ => The_setup_response_should_be_created(),
            _ => The_reservation_is_cancelled_for_setup(),
            _ => The_setup_cancellation_should_succeed());
    }

    private async Task The_reservation_is_cancelled_for_setup()
        => await _cancelSteps.Send(_createdReservationId);

    private async Task The_setup_cancellation_should_succeed()
        => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    #endregion

    #region When

    private async Task The_reservation_is_submitted()
        => await _postSteps.Send();

    private async Task The_reservation_is_retrieved_by_id()
        => await _getSteps.RetrieveById(_createdReservationId);

    private async Task The_reservation_is_cancelled()
        => await _cancelSteps.Send(_createdReservationId);

    private async Task The_reservation_is_cancelled_again()
        => await _cancelSteps.Send(_createdReservationId);

    private async Task The_reservation_is_deleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"reservations/{_createdReservationId}");
        request.Headers.Add(Constants.CustomHeaders.ComponentTestRequestId, RequestId);
        _deleteResponse = await Client.SendAsync(request);
    }

    private HttpResponseMessage? _deleteResponse;

    #endregion

    #region Then

    private async Task<CompositeStep> The_reservation_response_should_contain_the_confirmed_booking()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_post_response_should_be_valid_json(),
            _ => The_reservation_status_should_be_confirmed(),
            _ => The_reservation_customer_name_should_match());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_reservation_status_should_be_confirmed()
        => _postSteps.Response!.Status.Should().Be("Confirmed");

    private async Task The_reservation_customer_name_should_match()
        => _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request.CustomerName);

    private async Task<CompositeStep> The_reservation_get_response_should_contain_the_reservation()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_get_response_should_be_valid_json(),
            _ => The_retrieved_reservation_should_match_the_created_booking());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_get_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_retrieved_reservation_should_match_the_created_booking()
    {
        _getSteps.Response!.Id.Should().Be(_createdReservationId);
        _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName);
        _getSteps.Response!.Status.Should().Be("Confirmed");
    }

    private async Task<CompositeStep> The_cancellation_response_should_indicate_the_reservation_is_cancelled()
    {
        return Sub.Steps(
            _ => The_cancel_response_http_status_should_be_ok(),
            _ => The_cancel_response_should_be_valid_json(),
            _ => The_cancelled_reservation_status_should_be_cancelled());
    }

    private async Task The_cancel_response_http_status_should_be_ok()
        => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_cancel_response_should_be_valid_json()
        => await _cancelSteps.ParseResponse();

    private async Task The_cancelled_reservation_status_should_be_cancelled()
        => _cancelSteps.Response!.Status.Should().Be("Cancelled");

    private async Task The_cancellation_response_should_indicate_a_conflict()
        => _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);

    private async Task The_reservation_delete_response_should_indicate_no_content()
        => _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    #endregion
}
