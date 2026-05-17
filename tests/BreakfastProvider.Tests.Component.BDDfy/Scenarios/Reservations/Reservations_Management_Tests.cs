using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Reservations;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Reservations;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Reservations;

public class Reservations_Management_Tests : BaseFixture
{
    private readonly PostReservationSteps _postSteps;
    private readonly GetReservationSteps _getSteps;
    private readonly CancelReservationSteps _cancelSteps;

    private int _createdReservationId;
    private HttpResponseMessage? _deleteResponse;

    public Reservations_Management_Tests()
    {
        _postSteps = Get<PostReservationSteps>();
        _getSteps = Get<GetReservationSteps>();
        _cancelSteps = Get<CancelReservationSteps>();
    }

    private TestReservationRequest CreateValidRequest() => new()
    {
        CustomerName = $"Customer-{Guid.NewGuid():N}",
        TableNumber = Random.Shared.Next(1, 50),
        PartySize = Random.Shared.Next(1, 10),
        ReservedAt = DateTime.UtcNow.AddHours(2),
        ContactPhone = "07700900000"
    };

    [Fact]
    [HappyPath]
    public void Creating_a_reservation_should_return_the_confirmed_reservation()
    {
        this.Given(x => x.A_valid_reservation_request_is_prepared())
            .When(x => x.The_reservation_is_submitted())
            .Then(x => x.The_response_should_contain_the_confirmed_booking())
            .BDDfy();
    }

    [Fact]
    public void Retrieving_an_existing_reservation_should_return_the_reservation()
    {
        this.Given(x => x.A_reservation_exists())
            .When(x => x.The_reservation_is_retrieved_by_id())
            .Then(x => x.The_get_response_should_contain_the_reservation())
            .BDDfy();
    }

    [Fact]
    public void Cancelling_a_reservation_should_return_the_cancelled_reservation()
    {
        this.Given(x => x.A_reservation_exists())
            .When(x => x.The_reservation_is_cancelled())
            .Then(x => x.The_cancellation_response_should_indicate_cancelled())
            .BDDfy();
    }

    [Fact]
    public void Cancelling_an_already_cancelled_reservation_should_return_a_conflict_response()
    {
        this.Given(x => x.A_cancelled_reservation_exists())
            .When(x => x.The_reservation_is_cancelled())
            .Then(x => x.The_cancellation_response_should_indicate_conflict())
            .BDDfy();
    }

    [Fact]
    public void Deleting_a_reservation_should_return_no_content()
    {
        this.Given(x => x.A_reservation_exists())
            .When(x => x.The_reservation_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_no_content())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_reservation_request_is_prepared()
    {
        _postSteps.Request = CreateValidRequest();
        await Task.CompletedTask;
    }

    private async Task The_reservation_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_response_should_contain_the_confirmed_booking()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Status.Should().Be("Confirmed");
        _postSteps.Response!.CustomerName.Should().Be(_postSteps.Request!.CustomerName);
    }

    private async Task A_reservation_exists()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReservationId = _postSteps.Response!.Id;
    }

    private async Task The_reservation_is_retrieved_by_id()
    {
        await _getSteps.RetrieveById(_createdReservationId);
    }

    private async Task The_get_response_should_contain_the_reservation()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response!.Id.Should().Be(_createdReservationId);
        _getSteps.Response!.CustomerName.Should().Be(_postSteps.Response!.CustomerName);
        _getSteps.Response!.Status.Should().Be("Confirmed");
    }

    private async Task The_reservation_is_cancelled()
    {
        await _cancelSteps.Send(_createdReservationId);
    }

    private async Task The_cancellation_response_should_indicate_cancelled()
    {
        _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _cancelSteps.ParseResponse();
        _cancelSteps.Response!.Status.Should().Be("Cancelled");
    }

    private async Task A_cancelled_reservation_exists()
    {
        _postSteps.Request = CreateValidRequest();
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReservationId = _postSteps.Response!.Id;
        await _cancelSteps.Send(_createdReservationId);
        _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private void The_cancellation_response_should_indicate_conflict()
    {
        _cancelSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task The_reservation_is_deleted()
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"reservations/{_createdReservationId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, RequestId);
        _deleteResponse = await Client.SendAsync(request);
    }

    private void The_delete_response_should_indicate_no_content()
    {
        _deleteResponse!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    #endregion
}
