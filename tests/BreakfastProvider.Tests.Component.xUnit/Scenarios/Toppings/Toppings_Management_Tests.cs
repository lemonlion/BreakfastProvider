using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.Toppings;

#pragma warning disable CS1998
public class Toppings_Management_Tests : BaseFixture
{
    private readonly GetToppingsSteps _getSteps;
    private readonly PostToppingsSteps _postSteps;

    public Toppings_Management_Tests()
    {
        _getSteps = Get<GetToppingsSteps>();
        _postSteps = Get<PostToppingsSteps>();
    }

    [Fact]
    [HappyPath]
    public async Task Toppings_endpoint_should_return_all_available_toppings()
    {
        // When the available toppings are requested
        await _getSteps.Retrieve();

        // Then the toppings response should contain the default toppings
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseResponse();
        Track.That(() => _getSteps.Response.Should().HaveCount(ToppingDefaults.ExpectedToppingCount));
        Track.That(() => _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries));
        Track.That(() => _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Blueberries));
        Track.That(() => _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.MapleSyrup));
        Track.That(() => _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.WhippedCream));
        Track.That(() => _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.ChocolateChips));
    }

    [Fact]
    public async Task Adding_a_new_topping_should_return_the_created_topping()
    {
        // Given a valid topping request
        _postSteps.Request = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };

        // When the new topping is submitted
        await _postSteps.Send();

        // Then the topping response should contain the created topping
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        Track.That(() => _postSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries));
        Track.That(() => _postSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory));
    }
}
