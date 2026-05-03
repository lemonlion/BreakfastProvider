using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Toppings;

#pragma warning disable CS1998
public class Toppings_Deletion_Tests : BaseFixture
{
    private readonly DeleteToppingSteps _deleteSteps;

    private static readonly Guid KnownRaspberryToppingId = ToppingDefaults.KnownRaspberryToppingId;

    public Toppings_Deletion_Tests()
    {
        _deleteSteps = Get<DeleteToppingSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Deleting_an_existing_topping_should_return_no_content()
    {
        // Given a known topping exists
        var toppingId = KnownRaspberryToppingId;

        // When the topping is deleted
        await _deleteSteps.Send(toppingId);

        // Then the delete response should indicate success
        Track.That(() => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NoContent));
    }

    [Fact]
    public async Task Deleting_a_non_existent_topping_should_return_not_found()
    {
        // Given a topping id that does not exist
        var toppingId = Guid.NewGuid();

        // When the topping is deleted
        await _deleteSteps.Send(toppingId);

        // Then the delete response should indicate not found
        Track.That(() => _deleteSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }
}
