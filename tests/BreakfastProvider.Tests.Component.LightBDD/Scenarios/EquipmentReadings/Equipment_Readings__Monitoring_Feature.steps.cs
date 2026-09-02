using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;
using BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.EquipmentReadings;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Equipment_Readings__Monitoring_Feature : BaseFixture
{
    private readonly PostEquipmentReadingSteps _postSteps;
    private readonly GetEquipmentReadingSteps _getSteps;
    private readonly DeleteEquipmentReadingSteps _deleteSteps;
    private string _equipmentId = string.Empty;
    private string _createdReadingId = string.Empty;

    public Equipment_Readings__Monitoring_Feature()
    {
        _postSteps = Get<PostEquipmentReadingSteps>();
        _getSteps = Get<GetEquipmentReadingSteps>();
        _deleteSteps = Get<DeleteEquipmentReadingSteps>();
    }

    #region Given

    private async Task A_valid_equipment_reading_request()
    {
        _equipmentId = $"Oven-{Guid.NewGuid():N}";
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = _equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };
    }

    private async Task<CompositeStep> An_equipment_reading_record_has_been_created()
    {
        return Sub.Steps(
            _ => A_valid_equipment_reading_request(),
            _ => The_equipment_reading_is_recorded(),
            _ => The_setup_response_should_be_created());
    }

    private async Task The_setup_response_should_be_created()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReadingId = _postSteps.Response!.ReadingId;
    }

    private async Task An_equipment_reading_request_with_a_missing_metric()
    {
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = null,
            Value = 180m,
            Unit = "celsius"
        };
    }

    private async Task An_equipment_reading_request_with_zero_value()
    {
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = "temperature",
            Value = 0,
            Unit = "celsius"
        };
    }

    #endregion

    #region When

    private async Task The_equipment_reading_is_recorded() => await _postSteps.Send();

    private async Task The_readings_are_listed_by_equipment() => await _getSteps.RetrieveByEquipment(_equipmentId);

    private async Task The_reading_is_deleted() => await _deleteSteps.Delete(_createdReadingId);

    #endregion

    #region Then

    private async Task The_reading_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.EquipmentId.Should().Be(_equipmentId);
        _postSteps.Response!.Value.Should().Be(180.5m);
        _postSteps.Response!.ReadingId.Should().NotBeNullOrEmpty();
    }

    private async Task The_reading_list_response_should_contain_the_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(r => r.ReadingId == _createdReadingId);
    }

    private async Task The_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_reading_should_no_longer_be_listed()
    {
        await _getSteps.RetrieveByEquipment(_equipmentId);
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().NotContain(r => r.ReadingId == _createdReadingId);
    }

    private async Task The_reading_post_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
