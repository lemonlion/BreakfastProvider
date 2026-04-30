namespace BreakfastProvider.Api.HttpClients;

public class CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-Id";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = httpContextAccessor.HttpContext?.Request.Headers[CorrelationIdHeader].FirstOrDefault();

        if (!string.IsNullOrEmpty(correlationId))
            request.Headers.TryAddWithoutValidation(CorrelationIdHeader, correlationId);

        return base.SendAsync(request, cancellationToken);
    }
}
