using Serilog.Context;

namespace AgriApp.Api.Middleware;

/// <summary>
/// Propagates X-Correlation-ID (from client or generated) into response headers, HttpContext.Items, and Serilog log context.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    public const string HeaderName = "X-Correlation-ID";
    public const string HttpContextItemKey = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Request.Headers[HeaderName].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(correlationId))
            correlationId = Guid.NewGuid().ToString("N");

        context.Response.Headers[HeaderName] = correlationId;
        context.Items[HttpContextItemKey] = correlationId;

        using (LogContext.PushProperty(HttpContextItemKey, correlationId))
        {
            await _next(context);
        }
    }
}
