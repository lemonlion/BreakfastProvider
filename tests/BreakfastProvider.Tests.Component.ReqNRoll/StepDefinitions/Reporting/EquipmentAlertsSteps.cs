using System.Net;
using BreakfastProvider.Tests.Component.ReqNRoll.Support;
using BreakfastProvider.Tests.Component.Shared.Common.Pancakes;
using BreakfastProvider.Tests.Component.Shared.Common.Reporting;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Reporting;

[Binding]
public class EquipmentAlertsSteps(
    PostPancakesSteps pancakeSteps,
    GraphQlReportingSteps graphQlSteps)
{
    [When("the equipment alerts are queried via graphql")]
    public async Task WhenTheEquipmentAlertsAreQueriedViaGraphql()
        => await graphQlSteps.QueryEquipmentAlerts(waitForBatchId: pancakeSteps.Response?.BatchId);

    [Then("the graphql response should contain the equipment alert record")]
    public async Task ThenTheGraphqlResponseShouldContainTheEquipmentAlertRecord()
    {
        graphQlSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await graphQlSteps.ParseEquipmentAlertsResponse();
        var batchId = pancakeSteps.Response!.BatchId;
        graphQlSteps.EquipmentAlerts.Should().Contain(a =>
            a.BatchId == batchId &&
            a.EquipmentName == "Griddle" &&
            a.AlertType == "UsageCycleCompleted");
    }
}
