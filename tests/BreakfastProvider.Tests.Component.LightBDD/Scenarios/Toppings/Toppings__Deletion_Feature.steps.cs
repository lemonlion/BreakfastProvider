using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

#pragma warning disable CS1998
public partial class Toppings__Deletion_Feature : BaseFixture
{
    private readonly DeleteToppingSteps _deleteSteps;
    private Guid _toppingId;

    // Well-known topping ID from the static list in ToppingsController
    private static readonly Guid KnownRaspberryToppingId = ToppingDefaults.KnownRaspberryToppingId;

    public Toppings__Deletion_Feature()
    {
        _deleteSteps = Get<DeleteToppingSteps>();
    }

    #region Given

    private async Task A_known_topping_exists()
    {
        _toppingId = KnownRaspberryToppingId;
    }

    private async Task A_topping_id_that_does_not_exist()
    {
        _toppingId = Guid.NewGuid();
    }

    #endregion

    #region When

    private async Task The_topping_is_deleted()
        => await _deleteSteps.Send(_toppingId);

    #endregion

    #region Then

    private async Task The_delete_response_should_indicate_success()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);

    private async Task The_delete_response_should_indicate_not_found()
        => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);

    #endregion
}
