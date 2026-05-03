using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using Reqnroll;

namespace BreakfastProvider.Tests.Component.ReqNRoll.StepDefinitions.Orders;

[Binding]
public class StatusUpdateNotFoundSteps(PatchOrderStatusSteps patchSteps)
{
    [When("a status update is sent for a non-existent order")]
    public async Task WhenAStatusUpdateIsSentForANonExistentOrder()
    {
        await patchSteps.Send(Guid.NewGuid(), "Preparing");
    }

    [Then("the status update response should indicate not found")]
    public void ThenTheStatusUpdateResponseShouldIndicateNotFound()
    {
        Track.That(() => patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }
}
