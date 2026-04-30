using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace BreakfastProvider.Tests.Component.Shared.Fakes.HttpFakes;

public class RequestCapturingHandler(
    FakeRequestStore store,
    IHttpContextAccessor httpContextAccessor,
    string clientName) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = httpContextAccessor.HttpContext?.Request.Headers[CustomHeaders.ComponentTestRequestId]
            .FirstOrDefault();

        if (requestId != null)
        {
            string? body = null;
            if (request.Content != null)
            {
                await request.Content.LoadIntoBufferAsync();
                body = await request.Content.ReadAsStringAsync(cancellationToken);
            }

            var headers = request.Headers
                .ToDictionary(h => h.Key, h => string.Join(", ", h.Value));

            store.Add(requestId, new CapturedHttpRequest(
                clientName, request.Method, request.RequestUri, headers, body));
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
