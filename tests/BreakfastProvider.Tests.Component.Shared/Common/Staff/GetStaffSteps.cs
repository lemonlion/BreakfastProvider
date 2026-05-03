using BreakfastProvider.Tests.Component.Shared.Constants;
using BreakfastProvider.Tests.Component.Shared.Models.Staff;
using BreakfastProvider.Tests.Component.Shared.Util;

namespace BreakfastProvider.Tests.Component.Shared.Common.Staff;

public class GetStaffSteps(RequestContext context)
{
    public HttpResponseMessage? ResponseMessage { get; private set; }
    public TestStaffMemberResponse? Response { get; private set; }
    public List<TestStaffMemberResponse>? ListResponse { get; private set; }

    public async Task RetrieveById(int id)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{Endpoints.Staff}/{id}");
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task RetrieveAll()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, Endpoints.Staff);
        request.Headers.Add(CustomHeaders.ComponentTestRequestId, context.RequestId);
        ResponseMessage = await context.Client.SendAsync(request);
    }

    public async Task ParseResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        Response = Json.Deserialize<TestStaffMemberResponse>(content)!;
    }

    public async Task ParseListResponse()
    {
        var content = await ResponseMessage!.Content.ReadAsStringAsync();
        Track.That(() => Json.IsValid(content).Should().BeTrue());
        ListResponse = Json.Deserialize<List<TestStaffMemberResponse>>(content)!;
    }
}
