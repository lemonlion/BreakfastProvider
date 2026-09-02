using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.OrderTimings;
using BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.OrderTimings;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Order_Timings__Analytics_Feature : BaseFixture
{
    private readonly PostOrderTimingSteps _postSteps;
    private readonly GetOrderTimingSteps _getSteps;
    private string _station = string.Empty;
    private string _createdTimingId = string.Empty;

    public Order_Timings__Analytics_Feature()
    {
        _postSteps = Get<PostOrderTimingSteps>();
        _getSteps = Get<GetOrderTimingSteps>();
    }

    #region Given

    private async Task A_valid_order_timing_request()
    {
        _station = $"Griddle-{Guid.NewGuid():N}";
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = _station,
            ItemType = "Pancakes",
            PrepSeconds = 42.5m
        };
    }

    private async Task<CompositeStep> An_order_timing_record_has_been_created()
    {
        return Sub.Steps(
            _ => A_valid_order_timing_request(),
            _ => The_order_timing_is_recorded(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdTimingId = _postSteps.Response!.TimingId;
    }

    private async Task An_order_timing_request_with_a_missing_station()
    {
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = null,
            ItemType = "Pancakes",
            PrepSeconds = 10m
        };
    }

    private async Task An_order_timing_request_with_zero_prep_seconds()
    {
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = "Griddle",
            ItemType = "Pancakes",
            PrepSeconds = 0
        };
    }

    #endregion

    #region When

    private async Task The_order_timing_is_recorded() => await _postSteps.Send();

    private async Task The_timings_are_listed_by_station() => await _getSteps.RetrieveByStation(_station);

    private async Task The_timing_summary_is_requested() => await _getSteps.RetrieveSummary();

    #endregion

    #region Then

    private async Task The_timing_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Station.Should().Be(_station);
        _postSteps.Response!.PrepSeconds.Should().Be(42.5m);
        _postSteps.Response!.TimingId.Should().NotBeNullOrEmpty();
    }

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

    private async Task The_timing_post_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
