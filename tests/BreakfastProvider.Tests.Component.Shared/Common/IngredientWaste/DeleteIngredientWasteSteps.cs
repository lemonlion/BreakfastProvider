using BreakfastProvider.Tests.Component.Shared.Common;
using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Common.IngredientWaste;

public class DeleteIngredientWasteSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }

    public async Task Delete(string wasteId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Endpoints.IngredientWaste}/{wasteId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }
}
