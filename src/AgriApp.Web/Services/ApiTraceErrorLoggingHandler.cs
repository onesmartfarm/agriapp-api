using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace AgriApp.Web.Services;

/// <summary>
/// On non-success API responses, logs <c>traceId</c> from ProblemDetails JSON (and echoes to the browser console via <see cref="Console"/>)
/// so it can be matched to server Serilog entries.
/// </summary>
public sealed class ApiTraceErrorLoggingHandler : DelegatingHandler
{
    private readonly ILogger<ApiTraceErrorLoggingHandler> _logger;

    public ApiTraceErrorLoggingHandler(ILogger<ApiTraceErrorLoggingHandler> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
            return response;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (mediaType is null || !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase))
            return response;

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var traceId = TryExtractTraceId(body);
        var correlationId = response.Headers.TryGetValues(CorrelationIdHeaderName, out var values)
            ? values.FirstOrDefault()
            : null;

        var message =
            $"Agri API error {(int)response.StatusCode} {response.ReasonPhrase}. " +
            $"TraceId (match server logs): {traceId ?? "(not in body)"}. " +
            $"CorrelationId: {correlationId ?? "(none)"}. " +
            $"{request.Method} {request.RequestUri}";

        Console.WriteLine(message);
        _logger.LogWarning("{Message}", message);

        response.Content = new StringContent(body, Encoding.UTF8, "application/json; charset=utf-8");
        return response;
    }

    private const string CorrelationIdHeaderName = "X-Correlation-ID";

    private static string? TryExtractTraceId(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("traceId", out var t))
                return t.GetString();
            if (root.TryGetProperty("TraceId", out var t2))
                return t2.GetString();
        }
        catch (JsonException)
        {
            // Not ProblemDetails JSON
        }

        return null;
    }
}
