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

        var correlationId = RequestMetadata.ResolveCorrelationId(httpContext, "payload-correlation");

        Assert.Equal("payload-correlation", correlationId);
        Assert.Equal("payload-correlation", httpContext.Response.Headers[RequestMetadata.CorrelationIdHeaderName].ToString());
    }

    [Fact]
    public void ResolveIdempotencyKey_ShouldFallBackToHeaderAndEchoResponseHeader()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers[RequestMetadata.IdempotencyKeyHeaderName] = new StringValues("idem-header");

        var idempotencyKey = RequestMetadata.ResolveIdempotencyKey(httpContext);

        Assert.Equal("idem-header", idempotencyKey);
        Assert.Equal("idem-header", httpContext.Response.Headers[RequestMetadata.IdempotencyKeyHeaderName].ToString());
    }
}
