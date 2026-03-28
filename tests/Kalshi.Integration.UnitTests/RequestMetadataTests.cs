using Kalshi.Integration.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Kalshi.Integration.UnitTests;

public sealed class RequestMetadataTests
{
    [Fact]
    public void ResolveCorrelationId_ShouldPreferExplicitRequestCorrelationIdAndWriteResponseHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[RequestMetadata.CorrelationIdHeaderName] = new StringValues("header-correlation");

        var correlationId = RequestMetadata.ResolveCorrelationId(httpContext, " payload-correlation ");

        Assert.Equal("payload-correlation", correlationId);
        Assert.Equal("payload-correlation", httpContext.Response.Headers[RequestMetadata.CorrelationIdHeaderName].ToString());
    }

    [Fact]
    public void ResolveCorrelationId_ShouldFallBackToHeaderBeforeTraceIdentifier()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-123"
        };
        httpContext.Request.Headers[RequestMetadata.CorrelationIdHeaderName] = new StringValues(" header-correlation ");

        var correlationId = RequestMetadata.ResolveCorrelationId(httpContext);

        Assert.Equal("header-correlation", correlationId);
        Assert.Equal("header-correlation", httpContext.Response.Headers[RequestMetadata.CorrelationIdHeaderName].ToString());
    }

    [Fact]
    public void ResolveCorrelationId_ShouldFallBackToTraceIdentifierWhenHeaderMissing()
    {
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "trace-456"
        };

        var correlationId = RequestMetadata.ResolveCorrelationId(httpContext);

        Assert.Equal("trace-456", correlationId);
        Assert.Equal("trace-456", httpContext.Response.Headers[RequestMetadata.CorrelationIdHeaderName].ToString());
    }

    [Fact]
    public void ResolveIdempotencyKey_ShouldFallBackToHeaderAndEchoResponseHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[RequestMetadata.IdempotencyKeyHeaderName] = new StringValues(" idem-header ");

        var idempotencyKey = RequestMetadata.ResolveIdempotencyKey(httpContext);

        Assert.Equal("idem-header", idempotencyKey);
        Assert.Equal("idem-header", httpContext.Response.Headers[RequestMetadata.IdempotencyKeyHeaderName].ToString());
    }

    [Fact]
    public void ResolveIdempotencyKey_ShouldUseFallbackWhenHeaderMissing()
    {
        var httpContext = new DefaultHttpContext();

        var idempotencyKey = RequestMetadata.ResolveIdempotencyKey(httpContext, " fallback-key ");

        Assert.Equal("fallback-key", idempotencyKey);
        Assert.Equal("fallback-key", httpContext.Response.Headers[RequestMetadata.IdempotencyKeyHeaderName].ToString());
    }

    [Fact]
    public void ResolveIdempotencyKey_ShouldReturnNullWhenNoValueExists()
    {
        var httpContext = new DefaultHttpContext();

        var idempotencyKey = RequestMetadata.ResolveIdempotencyKey(httpContext);

        Assert.Null(idempotencyKey);
        Assert.False(httpContext.Response.Headers.ContainsKey(RequestMetadata.IdempotencyKeyHeaderName));
    }

    [Fact]
    public void MarkReplay_ShouldSetReplayHeader()
    {
        var httpContext = new DefaultHttpContext();

        RequestMetadata.MarkReplay(httpContext);

        Assert.Equal("true", httpContext.Response.Headers[RequestMetadata.IdempotentReplayHeaderName].ToString());
    }
}
