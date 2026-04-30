using System.Diagnostics;

namespace BreakfastProvider.Api.Filters;

public class CorrelationIdMiddleware(RequestDelegate next)
{
    public const string CorrelationIdHeader = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Headers.ContainsKey(CorrelationIdHeader))
            context.Request.Headers.Append(CorrelationIdHeader, Guid.NewGuid().ToString());

        var correlationId = context.Request.Headers[CorrelationIdHeader].ToString();
        context.Response.Headers.Append(CorrelationIdHeader, correlationId);

        Activity.Current?.SetTag("correlation.id", correlationId);

        await next(context);
    }
}
