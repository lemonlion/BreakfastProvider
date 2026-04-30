using BreakfastProvider.Tests.Component.Shared.Constants;
using Microsoft.AspNetCore.Http;

namespace BreakfastProvider.Tests.Component.LightBDD.Fakes.HttpFakes;

public class FakeHeaderPropagationHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var headers = httpContextAccessor.HttpContext?.Request.Headers;
        if (headers != null)
        {
            foreach (var header in headers.Where(h => h.Key.StartsWith("X-Fake-", StringComparison.OrdinalIgnoreCase)))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }

            if (headers.TryGetValue(CustomHeaders.ComponentTestRequestId, out var requestId))
            {
                request.Headers.TryAddWithoutValidation(CustomHeaders.ComponentTestRequestId, requestId.ToArray());
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}
