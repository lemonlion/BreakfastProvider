using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

using TestStack.BDDfy;
using Kronikol.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.DailySpecials;

public class DailySpecials_Idempotency_Tests : BaseFixture
{
    private readonly PostDailySpecialOrderSteps _postSteps;
    private readonly ResetDailySpecialOrdersSteps _resetSteps;

    private string _idempotencyKey = null!;
    private Guid _firstConfirmationId;
    private Guid _secondConfirmationId;

    public DailySpecials_Idempotency_Tests()
    {
        _postSteps = Get<PostDailySpecialOrderSteps>();
        _resetSteps = Get<ResetDailySpecialOrdersSteps>();
    }

    [Fact]
    public void Same_order_with_same_idempotency_key_should_return_same_confirmation()
    {
        this.Given(x => x.The_cinnamon_swirl_order_count_is_reset())
            .And(x => x.An_order_request_with_an_idempotency_key())
            .When(x => x.The_order_is_submitted_twice_with_the_same_idempotency_key())
            .Then(x => x.Both_responses_should_return_the_same_confirmation_id())
            .BDDfy();
    }

    [Fact]
    public void Same_order_with_different_idempotency_keys_should_return_different_confirmations()
    {
        this.Given(x => x.The_cinnamon_swirl_order_count_is_reset())
            .And(x => x.An_order_request_for_the_same_special())
            .When(x => x.The_order_is_submitted_with_two_different_idempotency_keys())
            .Then(x => x.The_responses_should_have_different_confirmation_ids())
            .BDDfy();
    }

    #region Steps

    private async Task The_cinnamon_swirl_order_count_is_reset()
    {
        await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);
    }

    private void An_order_request_with_an_idempotency_key()
    {
        _idempotencyKey = Guid.NewGuid().ToString();
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    private async Task The_order_is_submitted_twice_with_the_same_idempotency_key()
    {
        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, _idempotencyKey);

        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _firstConfirmationId = _postSteps.Response!.OrderConfirmationId;

        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _secondConfirmationId = _postSteps.Response!.OrderConfirmationId;
    }

    private void Both_responses_should_return_the_same_confirmation_id()
    {
        _firstConfirmationId.Should().Be(_secondConfirmationId);
    }

    private void An_order_request_for_the_same_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };
    }

    private async Task The_order_is_submitted_with_two_different_idempotency_keys()
    {
        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _firstConfirmationId = _postSteps.Response!.OrderConfirmationId;

        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, Guid.NewGuid().ToString());
        await _postSteps.Send();
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created);
        await _postSteps.ParseResponse();
        _secondConfirmationId = _postSteps.Response!.OrderConfirmationId;
    }

    private void The_responses_should_have_different_confirmation_ids()
    {
        _firstConfirmationId.Should().NotBe(_secondConfirmationId);
    }

    #endregion
}
