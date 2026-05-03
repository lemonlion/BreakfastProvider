using System.Net;
using BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;
using BreakfastProvider.Tests.Component.Shared.Models.DailySpecials;

namespace BreakfastProvider.Tests.Component.xUnit.Scenarios.DailySpecials;

public class DailySpecials_Not_Found_Tests : BaseFixture
{
    private readonly PostDailySpecialOrderSteps _postSteps;

    public DailySpecials_Not_Found_Tests()
    {
        _postSteps = Get<PostDailySpecialOrderSteps>();
    }

    [Fact]
    public async Task Ordering_non_existent_daily_special_should_return_not_found()
    {
        // Given a daily special order request for a non-existent special
        _postSteps.Request = new TestDailySpecialOrderRequest
        {
            SpecialId = Guid.NewGuid(),
            Quantity = 1
        };

        // When the daily special order is submitted
        await _postSteps.Send();

        // Then the response should indicate not found
        Track.That(() => _postSteps.ResponseMessage!.StatusCode.Should().Be(HttpStatusCode.NotFound));
    }
}
