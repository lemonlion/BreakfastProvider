using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.OrderTimings;
using BreakfastProvider.Tests.Component.Shared.Models.OrderTimings;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.OrderTimings;

[Binding]
public class OrderTimingsAnalyticsSteps(
    AppManager appManager,
    PostOrderTimingSteps postSteps,
    GetOrderTimingSteps getSteps)
{
    private string _station = string.Empty;
    private string _createdTimingId = string.Empty;

    [Given("a valid order timing request")]
    public void GivenAValidOrderTimingRequest()
    {
        _station = $"Griddle-{Guid.NewGuid():N}";
        postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = _station,
            ItemType = "Pancakes",
            PrepSeconds = 42.5m
        };
    }

    [Given("an order timing record has been created")]
    public async Task GivenAnOrderTimingRecordHasBeenCreated()
    {
        GivenAValidOrderTimingRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdTimingId = postSteps.Response!.TimingId;
    }

    [Given("an order timing request with a missing station")]
    public void GivenAnOrderTimingRequestWithAMissingStation()
    {
        postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = null,
            ItemType = "Pancakes",
            PrepSeconds = 10m
        };
    }

    [Given("an order timing request with zero prep seconds")]
    public void GivenAnOrderTimingRequestWithZeroPrepSeconds()
    {
        postSteps.Request = new TestOrderTimingRequest
        {
            OrderId = Guid.NewGuid().ToString(),
            Station = "Griddle",
            ItemType = "Pancakes",
            PrepSeconds = 0
        };
    }

    [When("the order timing is recorded")]
    public async Task WhenTheOrderTimingIsRecorded()
    {
        await postSteps.Send();
    }

    [When("the timings are listed by station")]
    public async Task WhenTheTimingsAreListedByStation()
    {
        await getSteps.RetrieveByStation(_station);
    }

    [When("the timing summary is requested")]
    public async Task WhenTheTimingSummaryIsRequested()
    {
        await getSteps.RetrieveSummary();
    }

    [Then("the timing response should contain the created record")]
    public async Task ThenTheTimingResponseShouldContainTheCreatedRecord()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.Station.Should().Be(_station);
        postSteps.Response!.PrepSeconds.Should().Be(42.5m);
        postSteps.Response!.TimingId.Should().NotBeNullOrEmpty();
    }

    [Then("the timing list response should contain the record")]
    public async Task ThenTheTimingListResponseShouldContainTheRecord()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(t => t.TimingId == _createdTimingId);
    }

    [Then("the summary should contain aggregated data for the station")]
    public async Task ThenTheSummaryShouldContainAggregatedDataForTheStation()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseSummaryResponse();
        getSteps.SummaryResponse!.Should().Contain(s => s.Station == _station && s.TimingCount >= 1);
    }

    [Then("the timing post response should indicate bad request")]
    public void ThenTheTimingPostResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
