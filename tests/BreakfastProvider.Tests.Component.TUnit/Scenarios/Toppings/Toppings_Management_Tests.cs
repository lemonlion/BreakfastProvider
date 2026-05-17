using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using Kronikol.TUnit;

namespace BreakfastProvider.Tests.Component.TUnit.Scenarios.Toppings;

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

    [Test]
    [HappyPath]
    public async Task Toppings_endpoint_should_return_all_available_toppings()
    {
        // When the available toppings are requested
        await _getSteps.Retrieve();

        // Then the toppings response should contain the default toppings
        await _getSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        await _getSteps.Response.Should().HaveCount(ToppingDefaults.ExpectedToppingCount);
        await _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
        await _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Blueberries);
        await _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.MapleSyrup);
        await _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.WhippedCream);
        await _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.ChocolateChips);
    }

    [Test]
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
        await _postSteps.ResponseMessage!.StatusCode.Should().BeEqualTo(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        await _postSteps.Response!.Name.Should().BeEqualTo(ToppingDefaults.Strawberries);
        await _postSteps.Response!.Category.Should().BeEqualTo(ToppingDefaults.FruitCategory);
    }
}
