using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;
using BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;
using BreakfastProvider.Tests.Component.TUnit.Infrastructure;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.EquipmentReadings;

public class EquipmentReadings_Monitoring_Tests : BaseFixture
{
    private readonly PostEquipmentReadingSteps _postSteps;
    private readonly GetEquipmentReadingSteps _getSteps;
    private readonly DeleteEquipmentReadingSteps _deleteSteps;

    public EquipmentReadings_Monitoring_Tests()
    {
        _postSteps = Get<PostEquipmentReadingSteps>();
        _getSteps = Get<GetEquipmentReadingSteps>();
        _deleteSteps = Get<DeleteEquipmentReadingSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Recording_an_equipment_reading_should_return_the_created_record()
    {
        // Given a valid equipment reading request
        var equipmentId = $"Oven-{Guid.NewGuid():N}";
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };

        // When the equipment reading is recorded
        await _postSteps.Send();

        // Then the reading response should contain the created record
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        await _postSteps.Response!.EquipmentId.Should().BeEqualTo(equipmentId);
        await _postSteps.Response!.Value.Should().BeEqualTo(180.5m);
        await _postSteps.Response!.ReadingId.Should().NotBeNull();
    }

    [Test]
    public async Task Listing_readings_by_equipment_should_return_matching_records()
    {
        // Given an equipment reading record has been created
        var equipmentId = $"Oven-{Guid.NewGuid():N}";
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdReadingId = _postSteps.Response!.ReadingId;

        // When the readings are listed by equipment
        await _getSteps.RetrieveByEquipment(equipmentId);

        // Then the reading list response should contain the record
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        await _getSteps.ListResponse!.Should().Contain(r => r.ReadingId == createdReadingId);
    }

    [Test]
    public async Task Deleting_a_reading_should_remove_it_from_the_list()
    {
        // Given an equipment reading record has been created
        var equipmentId = $"Oven-{Guid.NewGuid():N}";
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };
        await _postSteps.Send();
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        var createdReadingId = _postSteps.Response!.ReadingId;

        // When the reading is deleted
        await _deleteSteps.Delete(createdReadingId);

        // Then the delete response should indicate no content
        await _deleteSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NoContent);

        // And the reading should no longer be listed
        await _getSteps.RetrieveByEquipment(equipmentId);
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        await _getSteps.ListResponse!.Where(r => r.ReadingId == createdReadingId).ToList().Should().BeEmpty();
    }

    [Test]
    public async Task Recording_a_reading_with_missing_metric_should_return_bad_request()
    {
        // Given an equipment reading request with a missing metric
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = null,
            Value = 180m,
            Unit = "celsius"
        };

        // When the equipment reading is recorded
        await _postSteps.Send();

        // Then the reading post response should indicate bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Recording_a_reading_with_zero_value_should_return_bad_request()
    {
        // Given an equipment reading request with zero value
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = "temperature",
            Value = 0,
            Unit = "celsius"
        };

        // When the equipment reading is recorded
        await _postSteps.Send();

        // Then the reading post response should indicate bad request
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.BadRequest);
    }
}
