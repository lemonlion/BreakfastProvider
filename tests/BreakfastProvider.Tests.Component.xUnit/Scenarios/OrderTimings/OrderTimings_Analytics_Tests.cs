using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.OrderTimings;
using BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;
using BreakfastProvider.Tests.Component.xUnit.Infrastructure;
using Kronikol.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.OrderTimings;

public class OrderTimings_Analytics_Tests : BaseFixture
{
    private readonly PostOrderTimingSteps _postSteps;
    private readonly GetOrderTimingSteps _getSteps;

    public OrderTimings_Analytics_Tests()
    {
        _postSteps = Get<PostOrderTimingSteps>();
        _getSteps = Get<GetOrderTimingSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Recording_an_order_timing_should_return_the_created_record()
    {
        // Given a valid order timing request
        var station = $"Griddle-{Guid.NewGuid():N}";
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = station,
            ItemType = "Pancakes",
            PrepSeconds = 42.5m
        };

        // When the order timing is recorded
        await _postSteps.Send();

        // Then the timing response should contain the created record
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Station.Should().Be(station);
        _postSteps.Response!.PrepSeconds.Should().Be(42.5m);
        _postSteps.Response!.TimingId.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Listing_timings_by_station_should_return_matching_records()
    {
        // Given an order timing record has been created
        var station = $"Griddle-{Guid.NewGuid():N}";
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = station,
            ItemType = "Waffles",
            PrepSeconds = 30m
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdTimingId = _postSteps.Response!.TimingId;

        // When the timings are listed by station
        await _getSteps.RetrieveByStation(station);

        // Then the timing list response should contain the record
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(t => t.TimingId == createdTimingId);
    }

    [Fact]
    public async Task Getting_the_timing_summary_should_return_aggregated_data()
    {
        // Given an order timing record has been created
        var station = $"Griddle-{Guid.NewGuid():N}";
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = station,
            ItemType = "Muffins",
            PrepSeconds = 60m
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

        // When the timing summary is requested
        await _getSteps.RetrieveSummary();

        // Then the summary should contain aggregated data for the station
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseSummaryResponse();
        _getSteps.SummaryResponse!.Should().Contain(s => s.Station == station && s.TimingCount >= 1);
    }

    [Fact]
    public async Task Recording_a_timing_with_missing_station_should_return_bad_request()
    {
        // Given an order timing request with a missing station
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = null,
            ItemType = "Pancakes",
            PrepSeconds = 10m
        };

        // When the order timing is recorded
        await _postSteps.Send();

        // Then the timing post response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Recording_a_timing_with_zero_prep_seconds_should_return_bad_request()
    {
        // Given an order timing request with zero prep seconds
        _postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = "Griddle",
            ItemType = "Pancakes",
            PrepSeconds = 0
        };

        // When the order timing is recorded
        await _postSteps.Send();

        // Then the timing post response should indicate bad request
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
