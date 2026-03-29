using System.Net;
using Kalshi.Integration.Infrastructure.Integrations.NodeGateway;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Kalshi.Integration.UnitTests;

public sealed class NodeGatewayClientTests
{
    [Fact]
    public async Task CorrelationPropagationHandler_ShouldCopyCorrelationIdFromIncomingRequest()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["x-correlation-id"] = "corr-123";
        var httpContextAccessor = new HttpContextAccessor { HttpContext = httpContext };
        var innerHandler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"ok\"}")
        });

        var handler = new CorrelationPropagationHandler(httpContextAccessor)
        {
            InnerHandler = innerHandler,
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:3001"),
        };

        var client = new NodeGatewayClient(
            httpClient,
            Options.Create(new NodeGatewayOptions { BaseUrl = "http://localhost:3001", HealthPath = "/health", TimeoutSeconds = 5, RetryAttempts = 0 }),
            NullLogger<NodeGatewayClient>.Instance);

        var result = await client.ProbeHealthAsync();

        Assert.True(result.Healthy);
        Assert.Equal("corr-123", result.CorrelationId);
        Assert.Equal("corr-123", innerHandler.LastRequest!.Headers.GetValues("x-correlation-id").Single());
    }

    [Fact]
    public async Task NodeGatewayClient_ShouldReturnUnhealthyResultForNonSuccessStatus()
    {
        var innerHandler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
        {
            Content = new StringContent("gateway down")
        });

        using var httpClient = new HttpClient(innerHandler)
        {
            BaseAddress = new Uri("http://localhost:3001"),
        };

        var client = new NodeGatewayClient(
            httpClient,
            Options.Create(new NodeGatewayOptions { BaseUrl = "http://localhost:3001", HealthPath = "/health", TimeoutSeconds = 5, RetryAttempts = 0 }),
            NullLogger<NodeGatewayClient>.Instance);

        var result = await client.ProbeHealthAsync();

        Assert.False(result.Healthy);
        Assert.Equal(502, result.StatusCode);
        Assert.Equal("gateway down", result.ResponseBody);
    }

    [Fact]
    public async Task CorrelationPropagationHandler_ShouldFallBackToCurrentActivityTraceId()
    {
        var httpContextAccessor = new HttpContextAccessor();
        var innerHandler = new CapturingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"status\":\"ok\"}")
        });

        var handler = new CorrelationPropagationHandler(httpContextAccessor)
        {
            InnerHandler = innerHandler,
        };

        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:3001"),
        };

        using var activity = new System.Diagnostics.Activity("test-request");
        activity.Start();

        var client = new NodeGatewayClient(
            httpClient,
            Options.Create(new NodeGatewayOptions { BaseUrl = "http://localhost:3001", HealthPath = "/health", TimeoutSeconds = 5, RetryAttempts = 0 }),
            NullLogger<NodeGatewayClient>.Instance);

        await client.ProbeHealthAsync();

        Assert.Equal(activity.TraceId.ToString(), innerHandler.LastRequest!.Headers.GetValues("x-correlation-id").Single());
    }

    [Fact]
    public async Task NodeGatewayClient_ShouldThrowWhenDependencyCallFails()
    {
        var innerHandler = new ThrowingHandler(new HttpRequestException("node gateway unavailable"));

        using var httpClient = new HttpClient(innerHandler)
        {
            BaseAddress = new Uri("http://localhost:3001"),
        };

        var client = new NodeGatewayClient(
            httpClient,
            Options.Create(new NodeGatewayOptions { BaseUrl = "http://localhost:3001", HealthPath = "/health", TimeoutSeconds = 5, RetryAttempts = 0 }),
            NullLogger<NodeGatewayClient>.Instance);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.ProbeHealthAsync());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responseFactory;

        public CapturingHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        {
            _responseFactory = responseFactory;
        }

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responseFactory(request));
        }
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        private readonly Exception _exception;

        public ThrowingHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw _exception;
        }
    }
}
