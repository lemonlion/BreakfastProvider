using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Ingredients;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using BreakfastProvider.Tests.Component.Shared.Models.Pancakes;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Reporting;

public class Reporting_Equipment_Alerts_Tests : BaseFixture
{
    private readonly GetMilkSteps _milkSteps;
    private readonly GetEggsSteps _eggsSteps;
    private readonly GetFlourSteps _flourSteps;
    private readonly PostPancakesSteps _pancakeSteps;
    private readonly GraphQlReportingSteps _graphQlSteps;

    public Reporting_Equipment_Alerts_Tests()
    {
        _milkSteps = Get<GetMilkSteps>();
        _eggsSteps = Get<GetEggsSteps>();
        _flourSteps = Get<GetFlourSteps>();
        _pancakeSteps = Get<PostPancakesSteps>();
        _graphQlSteps = Get<GraphQlReportingSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Equipment_alerts_should_contain_data_ingested_via_event_hub_consumer()
    {
        // Given a pancake batch has been created
        await _milkSteps.Retrieve();
        Track.That(() => _milkSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _eggsSteps.Retrieve();
        Track.That(() => _eggsSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _flourSteps.Retrieve();
        Track.That(() => _flourSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));

        _pancakeSteps.Request = new TestPancakeRequest
        {
            Milk = _milkSteps.MilkResponse.Milk,
            Eggs = _eggsSteps.EggsResponse.Eggs,
            Flour = _flourSteps.FlourResponse.Flour
        };
        await _pancakeSteps.Send();
        Track.That(() => _pancakeSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _pancakeSteps.ParseResponse();
        Track.That(() => _pancakeSteps.Response.Should().NotBeNull());
        Track.That(() => _pancakeSteps.Response!.BatchId.Should().NotBeEmpty());

        // When the equipment alerts are queried via GraphQL
        await _graphQlSteps.QueryEquipmentAlerts(waitForBatchId: _pancakeSteps.Response?.BatchId);

        // Then the response should contain the equipment alert record
        Track.That(() => _graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _graphQlSteps.ParseEquipmentAlertsResponse();
        var batchId = _pancakeSteps.Response!.BatchId;
        Track.That(() => _graphQlSteps.EquipmentAlerts.Should().Contain(a =>
            a.BatchId == batchId &&
            a.EquipmentName == "Griddle" &&
            a.AlertType == "UsageCycleCompleted"));
    }
}
