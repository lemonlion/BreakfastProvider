using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using LightBDD.Framework;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using BreakfastProvider.Tests.Component.LightBDD.Util;


namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class DailySpecials__Ordering_Feature : BaseFixture
{
    private readonly GetDailySpecialsSteps _getSteps;
    private readonly PostDailySpecialOrderSteps _postSteps;
    private readonly ResetDailySpecialOrdersSteps _resetSteps;

    private DailySpecialsConfig? _dailySpecialsConfig;
    private DailySpecialsConfig DailySpecialsConfig => _dailySpecialsConfig ??=
        AppFactory.Services.GetRequiredService<IOptions<DailySpecialsConfig>>().Value;
    private int MaxOrdersPerSpecial => DailySpecialsConfig.MaxOrdersPerSpecial;

    public DailySpecials__Ordering_Feature()
    {
        _getSteps = Get<GetDailySpecialsSteps>();
        _postSteps = Get<PostDailySpecialOrderSteps>();
        _resetSteps = Get<ResetDailySpecialOrdersSteps>();
    }

    #region Given

    private async Task The_cinnamon_swirl_order_count_is_reset()
        => await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

    private async Task The_matcha_waffles_order_count_is_reset()
        => await _resetSteps.Reset(DailySpecialDefaults.MatchaWafflesId);

    private async Task The_lemon_ricotta_order_count_is_reset()
        => await _resetSteps.Reset(DailySpecialDefaults.LemonRicottaId);

    private async Task A_valid_daily_special_order_request_for_cinnamon_swirl()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    private async Task The_matcha_waffles_special_has_been_ordered_up_to_the_configured_limit()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = MaxOrdersPerSpecial
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private async Task A_daily_special_order_for_lemon_ricotta_of_quantity_one_is_placed()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.LemonRicottaId,
            Quantity = 1
        };
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region When

    private async Task The_daily_special_order_is_submitted()
        => await _postSteps.Send();

    private async Task Another_order_is_placed_for_the_matcha_waffles_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = 1
        };
        await _postSteps.Send();
    }

    private async Task The_available_daily_specials_are_requested()
        => await _getSteps.Retrieve();

    #endregion

    #region Then

    private async Task<CompositeStep> The_daily_special_order_response_should_contain_a_valid_confirmation()
    {
        return Sub.Steps(
            _ => The_post_response_http_status_should_be_created(),
            _ => The_order_response_should_be_valid_json(),
            _ => The_order_response_should_contain_the_correct_special_id(),
            _ => The_order_response_should_have_a_valid_confirmation_id());
    }

    private async Task The_post_response_http_status_should_be_created()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);

    private async Task The_order_response_should_be_valid_json()
        => await _postSteps.ParseResponse();

    private async Task The_order_response_should_contain_the_correct_special_id()
        => _postSteps.Response!.SpecialId.Should().Be(DailySpecialDefaults.CinnamonSwirlId);

    private async Task The_order_response_should_have_a_valid_confirmation_id()
        => _postSteps.Response!.OrderConfirmationId.Should().NotBeEmpty();

    private async Task<CompositeStep> The_daily_specials_response_should_contain_all_expected_specials()
    {
        return Sub.Steps(
            _ => The_get_response_http_status_should_be_ok(),
            _ => The_daily_specials_response_should_be_valid_json(),
            _ => The_daily_specials_list_should_contain_all_expected_specials());
    }

    private async Task The_get_response_http_status_should_be_ok()
        => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);

    private async Task The_daily_specials_response_should_be_valid_json()
        => await _getSteps.ParseResponse();

    private async Task The_daily_specials_list_should_contain_all_expected_specials()
        => _getSteps.Response.Should().HaveCount(DailySpecialDefaults.ExpectedSpecialsCount);

    private async Task The_response_should_indicate_the_daily_special_is_sold_out()
        => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);

    private async Task The_lemon_ricotta_special_should_have_one_fewer_remaining()
    {
        await _getSteps.ParseResponse();
        var lemonRicotta = _getSteps.Response!.Single(s => s.SpecialId == DailySpecialDefaults.LemonRicottaId);
        lemonRicotta.RemainingQuantity.Should().Be(MaxOrdersPerSpecial - 1);
    }

    #endregion
}
