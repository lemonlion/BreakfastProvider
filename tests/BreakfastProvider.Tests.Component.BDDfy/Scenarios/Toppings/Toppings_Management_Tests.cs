using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.Toppings;

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
    public void Toppings_endpoint_should_return_all_available_toppings()
    {
        this.When(x => x.The_available_toppings_are_requested())
            .Then(x => x.The_toppings_response_should_contain_the_default_toppings())
            .BDDfy();
    }

    [Fact]
    public void Adding_a_new_topping_should_return_the_created_topping()
    {
        this.Given(x => x.A_valid_topping_request())
            .When(x => x.The_new_topping_is_submitted())
            .Then(x => x.The_topping_response_should_contain_the_created_topping())
            .BDDfy();
    }

    #region Steps

    private async Task The_available_toppings_are_requested()
    {
        await _getSteps.Retrieve();
    }

    private async Task The_toppings_response_should_contain_the_default_toppings()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response.Should().HaveCount(ToppingDefaults.ExpectedToppingCount);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Blueberries);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.MapleSyrup);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.WhippedCream);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.ChocolateChips);
    }

    private void A_valid_topping_request()
    {
        _postSteps.Request = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    private async Task The_new_topping_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_topping_response_should_contain_the_created_topping()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries);
        _postSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory);
    }

    #endregion
}
