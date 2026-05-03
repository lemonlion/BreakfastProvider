using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.DailySpecials;

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
    public async Task Same_order_with_same_idempotency_key_should_return_same_confirmation()
    {
        // Given the cinnamon swirl order count is reset
        await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

        // And an order request with an idempotency key
        _idempotencyKey = Guid.NewGuid().ToString();
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        // When the order is submitted twice with the same idempotency key
        _postSteps.AddHeader(CustomHeaders.IdempotencyKey, _idempotencyKey);

        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _firstConfirmationId = _postSteps.Response!.OrderConfirmationId;

        await _postSteps.Send();
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.Created));
        await _postSteps.ParseResponse();
        _secondConfirmationId = _postSteps.Response!.OrderConfirmationId;

        // Then both responses should return the same confirmation id
        Track.That(() => _firstConfirmationId.Should().Be(_secondConfirmationId));
    }

    [Fact]
    public async Task Same_order_with_different_idempotency_keys_should_return_different_confirmations()
    {
        // Given the cinnamon swirl order count is reset
        await _resetSteps.Reset(DailySpecialDefaults.CinnamonSwirlId);

        // And an order request for the same special
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = DailySpecialDefaults.CinnamonSwirlId,
            Quantity = 1
        };

        // When the order is submitted with two different idempotency keys
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

        // Then the responses should have different confirmation ids
        Track.That(() => _firstConfirmationId.Should().NotBe(_secondConfirmationId));
    }
}
