using BreakfastProvider.Tests.Component.Shared.Constants;

namespace BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;

public class DeleteEquipmentReadingSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }

    public async Task Delete(string readingId)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"{Endpoints.EquipmentReadings}/{readingId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }
}
