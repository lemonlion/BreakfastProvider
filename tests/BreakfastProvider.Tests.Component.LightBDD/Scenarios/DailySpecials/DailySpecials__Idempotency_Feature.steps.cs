using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
public partial class DailySpecials__Idempotency_Feature : BaseFixture
{
    private readonly PostDailySpecialOrderSteps _postSteps;
    private readonly ResetDailySpecialOrdersSteps _resetSteps;

    private string _idempotencyKey = null!;
    private Guid _firstConfirmationId;
    private Guid _secondConfirmationId;

    public DailySpecials__Idempotency_Feature()
    {
        _postSteps = Get<PostDailySpecialOrderSteps>();
        _resetSteps = Get<ResetDailySpecialOrdersSteps>();
    }

    #region Given

    private async Task The_cinnamon_swirl_order_count_is_reset()
        => await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

    private async Task An_order_request_with_an_idempotency_key()
    {
        _idempotencyKey = Guid.NewGuid().ToString();
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    private async Task An_order_request_for_the_same_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    #endregion

    #region When

    private async Task The_order_is_submitted_twice_with_the_same_idempotency_key()
    {
        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, _idempotencyKey);

        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _firstConfirmationId = _postSteps.Response!.OrderConfirmationId;

        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _secondConfirmationId = _postSteps.Response!.OrderConfirmationId;
    }

    private async Task The_order_is_submitted_with_two_different_idempotency_keys()
    {
        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _firstConfirmationId = _postSteps.Response!.OrderConfirmationId;

        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _secondConfirmationId = _postSteps.Response!.OrderConfirmationId;
    }

    #endregion

    #region Then

    private async Task Both_responses_should_return_the_same_confirmation_id()
        => Track.That(() => _firstConfirmationId.Should().Be(_secondConfirmationId));

    private async Task The_responses_should_have_different_confirmation_ids()
        => Track.That(() => _firstConfirmationId.Should().NotBe(_secondConfirmationId));

    #endregion
}
