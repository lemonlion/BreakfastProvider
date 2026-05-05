using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Orders;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Orders;

#pragma warning disable CS1998
public partial class Orders__Status_Update_Not_Found_Feature : BaseFixture
{
    private readonly PatchOrderStatusSteps _patchSteps;
    private Guid _nonExistentOrderId;

    public Orders__Status_Update_Not_Found_Feature()
    {
        _patchSteps = Get<PatchOrderStatusSteps>();
    }

    #region Given

    private async Task A_non_existent_order_id()
    {
        _nonExistentOrderId = Guid.NewGuid();
    }

    #endregion

    #region When

    private async Task The_order_status_is_updated_to_preparing()
        => await _patchSteps.Send(_nonExistentOrderId, OrderStatuses.Preparing);

    #endregion

    #region Then

    private async Task The_response_should_indicate_not_found()
        => _patchSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    #endregion
}
