using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

using TestStack.BDDfy;
using TestTrackingDiagrams.BDDfy.xUnit3;
namespace BreakfastProvider.Tests.Component.BDDfy.Scenarios.DailySpecials;

public class DailySpecials_Not_Found_Tests : BaseFixture
{
    private readonly PostDailySpecialOrderSteps _postSteps;

    public DailySpecials_Not_Found_Tests()
    {
        _postSteps = Get<PostDailySpecialOrderSteps>();
    }

    [Fact]
    public void Ordering_non_existent_daily_special_should_return_not_found()
    {
        this.Given(x => x.A_daily_special_order_request_for_a_non_existent_special())
            .When(x => x.The_daily_special_order_is_submitted())
            .Then(x => x.The_response_should_indicate_not_found())
            .BDDfy();
    }

    #region Steps

    private void A_daily_special_order_request_for_a_non_existent_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = Guid.NewGuid(),
            Quantity = 1
        };
    }

    private async Task The_daily_special_order_is_submitted()
    {
        await _postSteps.Send();
    }

    private void The_response_should_indicate_not_found()
    {
        _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion
}
