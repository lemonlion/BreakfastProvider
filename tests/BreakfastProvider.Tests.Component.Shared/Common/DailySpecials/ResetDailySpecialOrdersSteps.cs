using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Common.DailySpecials;

public class ResetDailySpecialOrdersSteps(RequestContext context)
{
    public async Task Reset(Guid? specialId = null)
    {
        var url = specialId.HasValue
            ? $"{Endpoints.DailySpecialsOrders}?specialId={specialId.Value}"
            : Endpoints.DailySpecialsOrders;
        var request = new HttpRequestMessage(HttpMethod.Delete, url);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        await context.Client.SendAsync(request);
    }
}
