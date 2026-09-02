using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;
using BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.EquipmentReadings;

[Binding]
public class EquipmentReadingsMonitoringSteps(
    AppManager appManager,
    PostEquipmentReadingSteps postSteps,
    GetEquipmentReadingSteps getSteps,
    DeleteEquipmentReadingSteps deleteSteps)
{
    private string _equipmentId = string.Empty;
    private string _createdReadingId = string.Empty;

    [Given("a valid equipment reading request")]
    public void GivenAValidEquipmentReadingRequest()
    {
        _equipmentId = $"Oven-{Guid.NewGuid():N}";
        postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = _equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };
    }

    [Given("an equipment reading record has been created")]
    public async Task GivenAnEquipmentReadingRecordHasBeenCreated()
    {
        GivenAValidEquipmentReadingRequest();
        await postSteps.Send();
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        _createdReadingId = postSteps.Response!.ReadingId;
    }

    [Given("an equipment reading request with a missing metric")]
    public void GivenAnEquipmentReadingRequestWithAMissingMetric()
    {
        postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = null,
            Value = 180m,
            Unit = "celsius"
        };
    }

    [Given("an equipment reading request with zero value")]
    public void GivenAnEquipmentReadingRequestWithZeroValue()
    {
        postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = "temperature",
            Value = 0,
            Unit = "celsius"
        };
    }

    [When("the equipment reading is recorded")]
    public async Task WhenTheEquipmentReadingIsRecorded()
    {
        await postSteps.Send();
    }

    [When("the readings are listed by equipment")]
    public async Task WhenTheReadingsAreListedByEquipment()
    {
        await getSteps.RetrieveByEquipment(_equipmentId);
    }

    [When("the reading is deleted")]
    public async Task WhenTheReadingIsDeleted()
    {
        await deleteSteps.Delete(_createdReadingId);
    }

    [Then("the reading response should contain the created record")]
    public async Task ThenTheReadingResponseShouldContainTheCreatedRecord()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await postSteps.ParseResponse();
        postSteps.Response!.EquipmentId.Should().Be(_equipmentId);
        postSteps.Response!.Value.Should().Be(180.5m);
        postSteps.Response!.ReadingId.Should().NotBeNullOrEmpty();
    }

    [Then("the reading list response should contain the record")]
    public async Task ThenTheReadingListResponseShouldContainTheRecord()
    {
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().Contain(r => r.ReadingId == _createdReadingId);
    }

    [Then("the reading delete response should indicate no content")]
    public void ThenTheDeleteResponseShouldIndicateNoContent()
    {
        deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Then("the reading should no longer be listed")]
    public async Task ThenTheReadingShouldNoLongerBeListed()
    {
        await getSteps.RetrieveByEquipment(_equipmentId);
        getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await getSteps.ParseListResponse();
        getSteps.ListResponse!.Should().NotContain(r => r.ReadingId == _createdReadingId);
    }

    [Then("the reading post response should indicate bad request")]
    public void ThenTheReadingPostResponseShouldIndicateBadRequest()
    {
        postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
