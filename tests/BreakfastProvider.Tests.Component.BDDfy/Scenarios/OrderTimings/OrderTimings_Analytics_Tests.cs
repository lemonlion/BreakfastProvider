using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.OrderTimings;
using BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.OrderTimings;

public class OrderTimings_Analytics_Tests : BaseFixture
{
    private readonly PostOrderTimingSteps _postSteps;
    private readonly GetOrderTimingSteps _getSteps;

    private string _station = null!;
    private string _createdTimingId = null!;

    public OrderTimings_Analytics_Tests()
    {
        _postSteps = Get<PostOrderTimingSteps>();
        _getSteps = Get<GetOrderTimingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Recording_an_order_timing_should_return_the_created_record()
    {
        this.Given(x => x.A_valid_order_timing_request_is_prepared())
            .When(x => x.The_order_timing_is_recorded())
            .Then(x => x.The_timing_response_should_contain_the_created_record())
            .BDDfy();
    }

    [Fact]
    public void Listing_timings_by_station_should_return_matching_records()
    {
        this.Given(x => x.An_order_timing_record_has_been_created())
            .When(x => x.The_timings_are_listed_by_station())
            .Then(x => x.The_timing_list_response_should_contain_the_record())
            .BDDfy();
    }

    [Fact]
    public void Getting_the_timing_summary_should_return_aggregated_data()
    {
        this.Given(x => x.An_order_timing_record_has_been_created())
            .When(x => x.The_timing_summary_is_requested())
            .Then(x => x.The_summary_should_contain_aggregated_data_for_the_station())
            .BDDfy();
    }

    [Fact]
    public void Recording_a_timing_with_missing_station_should_return_bad_request()
    {
        this.Given(x => x.An_order_timing_request_with_a_missing_station_is_prepared())
            .When(x => x.The_order_timing_is_recorded())
            .Then(x => x.The_timing_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Recording_a_timing_with_zero_prep_seconds_should_return_bad_request()
    {
        this.Given(x => x.An_order_timing_request_with_zero_prep_seconds_is_prepared())
            .When(x => x.The_order_timing_is_recorded())
            .Then(x => x.The_timing_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_order_timing_request_is_prepared()
    {
        _station = $"Griddle-{Guid.NewGuid():N}";
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = _station,
            ItemType = "Pancakes",
            PrepSeconds = 42.5m
        };
        await Task.CompletedTask;
    }

    private async Task The_order_timing_is_recorded() => await _postSteps.Send();

    private async Task The_timing_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Station.Should().Be(_station);
        _postSteps.Response!.PrepSeconds.Should().Be(42.5m);
        _postSteps.Response!.TimingId.Should().NotBeNullOrEmpty();
    }

    private async Task An_order_timing_record_has_been_created()
    {
        await A_valid_order_timing_request_is_prepared();
        await The_order_timing_is_recorded();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdTimingId = _postSteps.Response!.TimingId;
    }

    private async Task The_timings_are_listed_by_station() => await _getSteps.RetrieveByStation(_station);

    private async Task The_timing_summary_is_requested() => await _getSteps.RetrieveSummary();

    private async Task The_timing_list_response_should_contain_the_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(t => t.TimingId == _createdTimingId);
    }

    private async Task The_summary_should_contain_aggregated_data_for_the_station()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseSummaryResponse();
        _getSteps.SummaryResponse!.Should().Contain(s => s.Station == _station && s.TimingCount >= 1);
    }

    private async Task An_order_timing_request_with_a_missing_station_is_prepared()
    {
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = null,
            ItemType = "Pancakes",
            PrepSeconds = 10m
        };
        await Task.CompletedTask;
    }

    private async Task An_order_timing_request_with_zero_prep_seconds_is_prepared()
    {
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = "Griddle",
            ItemType = "Pancakes",
            PrepSeconds = 0
        };
        await Task.CompletedTask;
    }

    private void The_timing_post_response_should_indicate_bad_request()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion
}
