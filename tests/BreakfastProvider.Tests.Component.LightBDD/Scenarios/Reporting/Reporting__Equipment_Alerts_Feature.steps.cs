using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Reporting;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Reporting__Equipment_Alerts_Feature : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;

    public Reporting__Equipment_Alerts_Feature()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    #region Given

    private async Task<CompositeStep> A_pancake_batch_has_been_created()
    {
        return Sub.Steps(
            _ => Milk_is_retrieved_from_the_milk_endpoint(),
            _ => The_milk_response_should_be_successful(),
            _ => Eggs_are_retrieved_from_the_eggs_endpoint(),
            _ => The_eggs_response_should_be_successful(),
            _ => Flour_is_retrieved_from_the_flour_endpoint(),
            _ => The_flour_response_should_be_successful(),
            _ => A_pancake_request_is_submitted_with_all_ingredients(),
            _ => The_pancake_batch_response_should_be_successful());
    }

    private async Task Milk_is_retrieved_from_the_milk_endpoint()
        => await _milkSteps.Retrieve();

    private async Task The_milk_response_should_be_successful()
        => Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Eggs_are_retrieved_from_the_eggs_endpoint()
        => await _eggsSteps.Retrieve();

    private async Task The_eggs_response_should_be_successful()
        => Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task Flour_is_retrieved_from_the_flour_endpoint()
        => await _flourSteps.Retrieve();

    private async Task The_flour_response_should_be_successful()
        => Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task A_pancake_request_is_submitted_with_all_ingredients()
    {
        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
    }

    private async Task The_pancake_batch_response_should_be_successful()
    {
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());
    }

    #endregion

    #region When

    private async Task The_equipment_alerts_are_queried_via_graphql()
        => await _graphQlSteps.QueryEquipmentAlerts(waitForBatchId: _pancakeSteps.Response?.BatchId);

    #endregion

    #region Then

    private async Task<CompositeStep> The_graphql_response_should_contain_the_equipment_alert_record()
    {
        return Sub.Steps(
            _ => The_equipment_alerts_response_should_be_successful(),
            _ => The_equipment_alerts_response_should_be_valid_json(),
            _ => The_equipment_alerts_should_contain_the_pancake_batch_alert());
    }

    private async Task The_equipment_alerts_response_should_be_successful()
        => Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

    private async Task The_equipment_alerts_response_should_be_valid_json()
        => await _graphQlSteps.ParseEquipmentAlertsResponse();

    private async Task The_equipment_alerts_should_contain_the_pancake_batch_alert()
    {
        var batchId = _pancakeSteps.Response!.BatchId;
        Track.That(() => _graphQlSteps.EquipmentAlerts.Should().Contain(a =>
            a.BatchId == batchId &&
            a.EquipmentName == "Griddle" &&
            a.AlertType == "UsageCycleCompleted"));
    }

    #endregion
}
