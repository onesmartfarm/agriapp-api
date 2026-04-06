using Microsoft.AspNetCore.Mvc;
using Serilog.Context;

namespace AgriApp.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC 7807 ProblemDetails JSON with traceId for cross-reference with client logs.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogError(ex, "Unhandled exception after response started.");
                throw;
            }

            var correlationId = context.Items[CorrelationIdMiddleware.HttpContextItemKey]?.ToString();
            using (LogContext.PushProperty(CorrelationIdMiddleware.HttpContextItemKey, correlationId))
            {
                _logger.LogError(ex, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
            }

            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json; charset=utf-8";

            var traceId = context.TraceIdentifier;
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred while processing your request.",
                Detail = _environment.IsDevelopment() ? ex.Message : null,
                Instance = context.Request.Path.Value
            };
            problem.Extensions["traceId"] = traceId;
            if (!string.IsNullOrEmpty(correlationId))
                problem.Extensions["correlationId"] = correlationId;

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
