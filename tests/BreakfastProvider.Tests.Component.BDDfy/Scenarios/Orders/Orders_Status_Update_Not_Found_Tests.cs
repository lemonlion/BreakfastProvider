using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Orders;

public class Orders_Status_Update_Not_Found_Tests : BaseFixture
{
    private readonly PatchOrderStatusSteps _patchSteps;
    private Guid _nonExistentOrderId;

    public Orders_Status_Update_Not_Found_Tests()
    {
        _patchSteps = Get<PatchOrderStatusSteps>();
    }

    [Fact]
    public void Updating_status_of_non_existent_order_should_return_not_found()
    {
        this.Given(x => x.A_non_existent_order_id())
            .When(x => x.The_order_status_is_updated_to_preparing())
            .Then(x => x.The_response_should_indicate_not_found())
            .BDDfy();
    }

    #region Steps

    private void A_non_existent_order_id()
    {
        _nonExistentOrderId = Guid.NewGuid();
    }

    private async Task The_order_status_is_updated_to_preparing()
    {
        await _patchSteps.Send(_nonExistentOrderId, OrderStatuses.Preparing);
    }

    private void The_response_should_indicate_not_found()
    {
        _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
