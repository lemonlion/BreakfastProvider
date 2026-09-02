using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.EquipmentReadings;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.EquipmentReadings;

public class GetEquipmentReadingSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public List<TestEquipmentReadingResponse>? ListResponse { get; private set; }

    public async Task RetrieveByEquipment(string equipmentId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.EquipmentReadings}/equipment/{equipmentId}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        var responseContentIsValidJson = Json.IsValid(content);
        responseContentIsValidJson.Should().BeTrue();
        ListResponse = Json.Deserialize<List<TestEquipmentReadingResponse>>(content)!;
    }
}
