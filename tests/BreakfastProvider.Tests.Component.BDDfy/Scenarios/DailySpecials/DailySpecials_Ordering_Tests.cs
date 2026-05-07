using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.DailySpecials;

public class DailySpecials_Ordering_Tests : BaseFixture
{
    private readonly GetDailySpecialsSteps _getSteps;
    private readonly PostDailySpecialOrderSteps _postSteps;
    private readonly ResetDailySpecialOrdersSteps _resetSteps;

    private DailySpecialsConfig? _dailySpecialsConfig;
    private DailySpecialsConfig DailySpecialsConfig => _dailySpecialsConfig ??=
        AppFactory.Services.GetRequiredService<IOptions<DailySpecialsConfig>>().Value;
    private int MaxOrdersPerSpecial => DailySpecialsConfig.MaxOrdersPerSpecial;

    public DailySpecials_Ordering_Tests()
    {
        _getSteps = Get<GetDailySpecialsSteps>();
        _postSteps = Get<PostDailySpecialOrderSteps>();
        _resetSteps = Get<ResetDailySpecialOrdersSteps>();
    }

    [Fact]
    [HappyPath]
    public void Valid_daily_special_order_should_return_a_confirmation()
    {
        this.Given(x => x.The_cinnamon_swirl_order_count_is_reset())
            .And(x => x.A_valid_daily_special_order_request_for_cinnamon_swirl())
            .When(x => x.The_daily_special_order_is_submitted())
            .Then(x => x.The_daily_special_order_response_should_contain_a_valid_confirmation())
            .BDDfy();
    }

    [Fact]
    public void Daily_specials_endpoint_should_return_all_available_specials()
    {
        this.When(x => x.The_available_daily_specials_are_requested())
            .Then(x => x.The_daily_specials_response_should_contain_all_expected_specials())
            .BDDfy();
    }

    [Fact]
    public void Ordering_daily_special_beyond_threshold_should_return_conflict()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_matcha_waffles_order_count_is_reset())
            .And(x => x.The_matcha_waffles_special_has_been_ordered_up_to_the_configured_limit())
            .When(x => x.Another_order_is_placed_for_the_matcha_waffles_special())
            .Then(x => x.The_response_should_indicate_the_daily_special_is_sold_out())
            .BDDfy();
    }

    [Fact]
    public void Remaining_quantity_should_decrease_after_each_order()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        this.Given(x => x.The_lemon_ricotta_order_count_is_reset())
            .And(x => x.A_daily_special_order_for_lemon_ricotta_of_quantity_one_is_placed())
            .When(x => x.The_available_daily_specials_are_requested())
            .Then(x => x.The_lemon_ricotta_special_should_have_one_fewer_remaining())
            .BDDfy();
    }

    #region Steps

    private async Task The_cinnamon_swirl_order_count_is_reset()
    {
        await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);
    }

    private void A_valid_daily_special_order_request_for_cinnamon_swirl()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    private async Task The_daily_special_order_is_submitted()
    {
        await _postSteps.Send();
    }

    private async Task The_daily_special_order_response_should_contain_a_valid_confirmation()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _postSteps.Response!.SpecialId.Should().Be(DailySpecialDefaults.CinnamonSwirlId);
        _postSteps.Response!.OrderConfirmationId.Should().NotBeEmpty();
    }

    private async Task The_available_daily_specials_are_requested()
    {
        await _getSteps.Retrieve();
    }

    private async Task The_daily_specials_response_should_contain_all_expected_specials()
    {
        _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK);
        await _getSteps.ParseResponse();
        _getSteps.Response.Should().HaveCount(DailySpecialDefaults.ExpectedSpecialsCount);
    }

    private async Task The_matcha_waffles_order_count_is_reset()
    {
        await _resetSteps.Reset(DailySpecialDefaults.MatchaWafflesId);
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

    private async Task Another_order_is_placed_for_the_matcha_waffles_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = 1
        };
        await _postSteps.Send();
    }

    private void The_response_should_indicate_the_daily_special_is_sold_out()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    private async Task The_lemon_ricotta_order_count_is_reset()
    {
        await _resetSteps.Reset(DailySpecialDefaults.LemonRicottaId);
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

    private async Task The_lemon_ricotta_special_should_have_one_fewer_remaining()
    {
        await _getSteps.ParseResponse();
        var lemonRicottaSpecial = _getSteps.Response!.Single(s => s.SpecialId == DailySpecialDefaults.LemonRicottaId);
        var lemonRicottaRemainingQuantity = lemonRicottaSpecial.RemainingQuantity;
        lemonRicottaRemainingQuantity.Should().Be(MaxOrdersPerSpecial - 1);
    }

    #endregion
}
