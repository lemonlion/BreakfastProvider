using System.Net;
using BreakfastProvider.Api.Configuration;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestTrackingDiagrams.xUnit3;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.DailySpecials;

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
    public async Task Valid_daily_special_order_should_return_a_confirmation()
    {
        // Given the cinnamon swirl order count is reset
        await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

        // And a valid daily special order request for cinnamon swirl
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        // When the daily special order is submitted
        await _postSteps.Send();

        // Then the daily special order response should contain a valid confirmation
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        Track.That(() => _postSteps.Response!.SpecialId.Should().Be(DailySpecialDefaults.CinnamonSwirlId));
        Track.That(() => _postSteps.Response!.OrderConfirmationId.Should().NotBeEmpty());
    }

    [Fact]
    public async Task Daily_specials_endpoint_should_return_all_available_specials()
    {
        // When the available daily specials are requested
        await _getSteps.Retrieve();

        // Then the daily specials response should contain all expected specials
        Track.That(() => _getSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.OK));
        await _getSteps.ParseResponse();
        Track.That(() => _getSteps.Response.Should().HaveCount(DailySpecialDefaults.ExpectedSpecialsCount));
    }

    [Fact]
    public async Task Ordering_daily_special_beyond_threshold_should_return_conflict()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the matcha waffles order count is reset
        await _resetSteps.Reset(DailySpecialDefaults.MatchaWafflesId);

        // And the matcha waffles special has been ordered up to the configured limit
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = MaxOrdersPerSpecial
        };
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

        // When another order is placed for the matcha waffles special
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.MatchaWafflesId,
            Quantity = 1
        };
        await _postSteps.Send();

        // Then the response should indicate the daily special is sold out
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Conflict));
    }

    [Fact]
    public async Task Remaining_quantity_should_decrease_after_each_order()
    {
        if (Settings.RunAgainstExternalServiceUnderTest)
            return;

        // Given the lemon ricotta order count is reset
        await _resetSteps.Reset(DailySpecialDefaults.LemonRicottaId);

        // And a daily special order for lemon ricotta of quantity one is placed
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.LemonRicottaId,
            Quantity = 1
        };
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));

        // When the available daily specials are requested
        await _getSteps.Retrieve();

        // Then the lemon ricotta special should have one fewer remaining
        await _getSteps.ParseResponse();
        var lemonRicottaSpecial = _getSteps.Response!.Single(s => s.SpecialId == DailySpecialDefaults.LemonRicottaId);
        var lemonRicottaRemainingQuantity = lemonRicottaSpecial.RemainingQuantity;
        Track.That(() => lemonRicottaRemainingQuantity.Should().Be(MaxOrdersPerSpecial - 1));
    }
}
