using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.Toppings;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Toppings;
using LightBDD.Framework;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.Toppings;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class Toppings__Management_Feature : BaseFixture
{
    private readonly GetToppingsSteps _getSteps;
    private readonly PostToppingsSteps _postSteps;

    public Toppings__Management_Feature()
    {
        _getSteps = Get<GetToppingsSteps>();
        _postSteps = Get<PostToppingsSteps>();
    }

    #region Given

    private async Task A_valid_topping_request()
    {
        _postSteps.Request = new TestToppingRequest
        {
            Name = ToppingDefaults.Strawberries,
            Category = ToppingDefaults.FruitCategory
        };
    }

    #endregion

    #region When

    private async Task The_available_toppings_are_requested()
        => await _getSteps.Retrieve();

    private async Task The_new_topping_is_submitted()
        => await _postSteps.Send();

    #endregion

    #region Then

    private async Task<CompositeStep> The_toppings_response_should_contain_the_default_toppings()
    {
        return Sub.Steps(
            _ => The_toppings_get_response_http_status_should_be_ok(),
            _ => The_toppings_list_should_be_valid_json(),
            _ => The_toppings_list_should_contain_the_expected_items());
    }

    private async Task The_toppings_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_toppings_list_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_toppings_list_should_contain_the_expected_items()
    {
        _getSteps.Response.Should().HaveCount(ToppingDefaults.ExpectedToppingCount);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Raspberries);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.Blueberries);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.MapleSyrup);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.WhippedCream);
        _getSteps.Response!.Should().Contain(t => t.Name == ToppingDefaults.ChocolateChips);
    }

    private async Task<CompositeStep> The_topping_response_should_contain_the_created_topping()
    {
        return Sub.Steps(
            _ => The_topping_post_response_http_status_should_be_created(),
            _ => The_topping_post_response_should_be_valid_json(),
            _ => The_created_topping_should_have_the_correct_name(),
            _ => The_created_topping_should_have_the_correct_category());
    }

    private async Task The_topping_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_topping_post_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_created_topping_should_have_the_correct_name()
        => _postSteps.Response!.Name.Should().Be(ToppingDefaults.Strawberries);

    private async Task The_created_topping_should_have_the_correct_category()
        => _postSteps.Response!.Category.Should().Be(ToppingDefaults.FruitCategory);

    #endregion
}
