using System.Diagnostics;

namespace Kalshi.Integration.Api.Infrastructure;

public sealed class RequestTimingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestTimingMiddleware> _logger;

    public RequestTimingMiddleware(RequestDelegate next, ILogger<RequestTimingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        var stopwatch = Stopwatch.StartNew();
        var request = httpContext.Request;
        var correlationId = httpContext.Request.Headers.TryGetValue(RequestMetadata.CorrelationIdHeaderName, out var headerValue)
            ? headerValue.ToString()
            : httpContext.TraceIdentifier;

        try
        {
            await _next(httpContext);
            stopwatch.Stop();

            _logger.LogInformation(
                "Request completed {Method} {Path} with statusCode={StatusCode} in {ElapsedMs} ms. correlationId={CorrelationId} traceId={TraceIdentifier}",
                request.Method,
                request.Path.Value ?? "/",
                httpContext.Response.StatusCode,
                stopwatch.Elapsed.TotalMilliseconds,
                correlationId,
                httpContext.TraceIdentifier);
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            _logger.LogError(
                exception,
                "Request failed {Method} {Path} after {ElapsedMs} ms. correlationId={CorrelationId} traceId={TraceIdentifier}",
                request.Method,
                request.Path.Value ?? "/",
                stopwatch.Elapsed.TotalMilliseconds,
                correlationId,
                httpContext.TraceIdentifier);

            throw;
        }
    }
}
