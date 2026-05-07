using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

public class Toppings_Deletion_Tests : BaseFixture
{
    private readonly DeleteToppingSteps _deleteSteps;

    private static readonly Guid KnownRaspberryToppingId = ToppingDefaults.KnownRaspberryToppingId;

    private Guid _toppingId;

    public Toppings_Deletion_Tests()
    {
        _deleteSteps = Get<DeleteToppingSteps>();
    }

    [Fact]
    [HappyPath]
    public void Deleting_an_existing_topping_should_return_no_content()
    {
        this.Given(x => x.A_known_topping_exists())
            .When(x => x.The_topping_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_success())
            .BDDfy();
    }

    [Fact]
    public void Deleting_a_non_existent_topping_should_return_not_found()
    {
        this.Given(x => x.A_topping_id_that_does_not_exist())
            .When(x => x.The_topping_is_deleted())
            .Then(x => x.The_delete_response_should_indicate_not_found())
            .BDDfy();
    }

    #region Steps

    private void A_known_topping_exists()
    {
        _toppingId = KnownRaspberryToppingId;
    }

    private void A_topping_id_that_does_not_exist()
    {
        _toppingId = Guid.NewGuid();
    }

    private async Task The_topping_is_deleted()
    {
        await _deleteSteps.Send(_toppingId);
    }

    private void The_delete_response_should_indicate_success()
    {
        _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private void The_delete_response_should_indicate_not_found()
    {
        _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
