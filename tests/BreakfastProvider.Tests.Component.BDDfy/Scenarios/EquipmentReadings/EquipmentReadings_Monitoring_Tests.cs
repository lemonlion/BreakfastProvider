using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;
using BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;
using BreakfastProvider.Tests.Component.BDDfy.Infrastructure;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;

namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.EquipmentReadings;

public class EquipmentReadings_Monitoring_Tests : BaseFixture
{
    private readonly PostEquipmentReadingSteps _postSteps;
    private readonly GetEquipmentReadingSteps _getSteps;
    private readonly DeleteEquipmentReadingSteps _deleteSteps;

    private string _equipmentId = null!;
    private string _createdReadingId = null!;

    public EquipmentReadings_Monitoring_Tests()
    {
        _postSteps = Get<PostEquipmentReadingSteps>();
        _getSteps = Get<GetEquipmentReadingSteps>();
        _deleteSteps = Get<DeleteEquipmentReadingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Recording_an_equipment_reading_should_return_the_created_record()
    {
        this.Given(x => x.A_valid_equipment_reading_request_is_prepared())
            .When(x => x.The_equipment_reading_is_recorded())
            .Then(x => x.The_reading_response_should_contain_the_created_record())
            .BDDfy();
    }

    [Fact]
    public void Listing_readings_by_equipment_should_return_matching_records()
    {
        this.Given(x => x.An_equipment_reading_record_has_been_created())
            .When(x => x.The_readings_are_listed_by_equipment())
            .Then(x => x.The_reading_list_response_should_contain_the_record())
            .BDDfy();
    }

    [Fact]
    public void Deleting_a_reading_should_remove_it_from_the_list()
    {
        this.Given(x => x.An_equipment_reading_record_has_been_created())
            .When(x => x.The_reading_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_no_content())
            .And(x => x.The_reading_should_no_longer_be_listed())
            .BDDfy();
    }

    [Fact]
    public void Recording_a_reading_with_missing_metric_should_return_bad_request()
    {
        this.Given(x => x.An_equipment_reading_request_with_a_missing_metric_is_prepared())
            .When(x => x.The_equipment_reading_is_recorded())
            .Then(x => x.The_reading_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    [Fact]
    public void Recording_a_reading_with_zero_value_should_return_bad_request()
    {
        this.Given(x => x.An_equipment_reading_request_with_zero_value_is_prepared())
            .When(x => x.The_equipment_reading_is_recorded())
            .Then(x => x.The_reading_post_response_should_indicate_bad_request())
            .BDDfy();
    }

    #region Steps

    private async Task A_valid_equipment_reading_request_is_prepared()
    {
        _equipmentId = $"Oven-{Guid.NewGuid():N}";
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = _equipmentId,
            Metric = "temperature",
            Value = 180.5m,
            Unit = "celsius"
        };
        await Task.CompletedTask;
    }

    private async Task The_equipment_reading_is_recorded() => await _postSteps.Send();

    private async Task The_reading_response_should_contain_the_created_record()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.EquipmentId.Should().Be(_equipmentId);
        _postSteps.Response!.Value.Should().Be(180.5m);
        _postSteps.Response!.ReadingId.Should().NotBeNullOrEmpty();
    }

    private async Task An_equipment_reading_record_has_been_created()
    {
        await A_valid_equipment_reading_request_is_prepared();
        await The_equipment_reading_is_recorded();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _createdReadingId = _postSteps.Response!.ReadingId;
    }

    private async Task The_readings_are_listed_by_equipment() => await _getSteps.RetrieveByEquipment(_equipmentId);

    private async Task The_reading_list_response_should_contain_the_record()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().Contain(r => r.ReadingId == _createdReadingId);
    }

    private async Task The_reading_is_deleted() => await _deleteSteps.Delete(_createdReadingId);

    private void The_delete_response_should_indicate_no_content()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_reading_should_no_longer_be_listed()
    {
        await _getSteps.RetrieveByEquipment(_equipmentId);
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseListResponse();
        _getSteps.ListResponse!.Should().NotContain(r => r.ReadingId == _createdReadingId);
    }

    private async Task An_equipment_reading_request_with_a_missing_metric_is_prepared()
    {
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = null,
            Value = 180m,
            Unit = "celsius"
        };
        await Task.CompletedTask;
    }

    private async Task An_equipment_reading_request_with_zero_value_is_prepared()
    {
        _postSteps.Request = new TestEquipmentReadingRequest
        {
            EquipmentId = "Oven-1",
            Metric = "temperature",
            Value = 0,
            Unit = "celsius"
        };
        await Task.CompletedTask;
    }

    private void The_reading_post_response_should_indicate_bad_request()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.BadRequest);

    #endregion
}
