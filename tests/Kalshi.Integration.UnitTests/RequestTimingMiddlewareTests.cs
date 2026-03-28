using Kalshi.Integration.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Kalshi.Integration.UnitTests;

public sealed class RequestTimingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldLogStructuredRequestTimingForSuccessfulRequests()
    {
        var logger = new TestLogger<RequestTimingMiddleware>();
        var middleware = new RequestTimingMiddleware(
            async context =>
            {
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                await Task.CompletedTask;
            },
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/health/live";
        httpContext.Request.Headers[RequestMetadata.CorrelationIdHeaderName] = new StringValues("corr-success");

        await middleware.InvokeAsync(httpContext);

        var logEntry = Assert.Single(logger.Entries.Where(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Information));
        Assert.Contains("Request completed GET /health/live", logEntry.Message, StringComparison.Ordinal);
        Assert.Contains("statusCode=204", logEntry.Message, StringComparison.Ordinal);
        Assert.Contains("correlationId=corr-success", logEntry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_ShouldLogErrorContextForFailedRequests()
    {
        var logger = new TestLogger<RequestTimingMiddleware>();
        var middleware = new RequestTimingMiddleware(
            _ => throw new InvalidOperationException("boom"),
            logger);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/v1/orders";
        httpContext.Request.Headers[RequestMetadata.CorrelationIdHeaderName] = new StringValues("corr-failure");

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(httpContext));

        var logEntry = Assert.Single(logger.Entries.Where(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Error));
        Assert.Contains("Request failed POST /api/v1/orders", logEntry.Message, StringComparison.Ordinal);
        Assert.Contains("correlationId=corr-failure", logEntry.Message, StringComparison.Ordinal);
        Assert.NotNull(logEntry.Exception);
    }
}
