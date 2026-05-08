using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Toppings;

#pragma warning disable CS1998
public class Toppings_Deletion_Tests : BaseFixture
{
    private readonly DeleteToppingSteps _deleteSteps;

    private static readonly Guid KnownRaspberryToppingId = ToppingDefaults.KnownRaspberryToppingId;

    public Toppings_Deletion_Tests()
    {
        _deleteSteps = Get<DeleteToppingSteps>();
    }

    [Test]
    [HappyPath]
    public async Task Deleting_an_existing_topping_should_return_no_content()
    {
        // Given a known topping exists
        var toppingId = KnownRaspberryToppingId;

        // When the topping is deleted
        await _deleteSteps.Send(toppingId);

        // Then the delete response should indicate success
        await _deleteSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Deleting_a_non_existent_topping_should_return_not_found()
    {
        // Given a topping id that does not exist
        var toppingId = Guid.NewGuid();

        // When the topping is deleted
        await _deleteSteps.Send(toppingId);

        // Then the delete response should indicate not found
        await _deleteSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.NotFound);
    }
}
