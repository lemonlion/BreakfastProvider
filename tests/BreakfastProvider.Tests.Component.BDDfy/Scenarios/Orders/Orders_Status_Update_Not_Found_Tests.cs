using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Status_Update_Not_Found_Tests : BaseFixture
{
    private readonly PatchOrderStatusSteps _patchSteps;

    public Orders_Status_Update_Not_Found_Tests()
    {
        _patchSteps = Get<PatchOrderStatusSteps>();
    }

    [Fact]
    public async Task Updating_status_of_non_existent_order_should_return_not_found()
    {
        // Given a non-existent order id
        var nonExistentOrderId = Guid.NewGuid();

        // When the order status is updated to preparing
        await _patchSteps.Send(nonExistentOrderId, OrderStatuses.Preparing);

        // Then the response should indicate not found
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
        this.BDDfy();
    }
}
