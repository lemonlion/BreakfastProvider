using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

namespace BreakfastProvider.Tests.Component.LightBDD.Scenarios.DailySpecials;

#pragma warning disable CS1998
public partial class DailySpecials__Not_Found_Feature : BaseFixture
{
    private readonly PostDailySpecialOrderSteps _postSteps;

    public DailySpecials__Not_Found_Feature()
    {
        _postSteps = Get<PostDailySpecialOrderSteps>();
    }

    #region Given

    private async Task A_daily_special_order_request_for_a_non_existent_special()
    {
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = Guid.NewGuid(),
            Quantity = 1
        };
    }

    #endregion

    #region When

    private async Task The_daily_special_order_is_submitted()
        => await _postSteps.Send();

    #endregion

    #region Then

    private async Task The_response_should_indicate_not_found()
        => Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));

    #endregion
}
