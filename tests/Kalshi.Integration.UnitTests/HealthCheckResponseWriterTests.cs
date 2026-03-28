using System.Text.Json;
using Kalshi.Integration.Api.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kalshi.Integration.UnitTests;

public sealed class HealthCheckResponseWriterTests
{
    [Fact]
    public async Task WriteJsonAsync_ShouldSerializeHealthReportPayload()
    {
        var entry = new HealthReportEntry(
            status: HealthStatus.Unhealthy,
            description: "Database unavailable",
            duration: TimeSpan.FromMilliseconds(18),
            exception: new InvalidOperationException("boom"),
            data: new Dictionary<string, object>());
        var report = new HealthReport(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = entry
            },
            TimeSpan.FromMilliseconds(25));

        var httpContext = new DefaultHttpContext();
        await using var responseStream = new MemoryStream();
        httpContext.Response.Body = responseStream;

        await HealthCheckResponseWriter.WriteJsonAsync(httpContext, report);

        responseStream.Position = 0;
        using var json = await JsonDocument.ParseAsync(responseStream);

        Assert.Equal("application/json", httpContext.Response.ContentType);
        Assert.Equal("Unhealthy", json.RootElement.GetProperty("status").GetString());
        Assert.Equal(25d, json.RootElement.GetProperty("totalDurationMs").GetDouble());
        var databaseEntry = json.RootElement.GetProperty("entries").GetProperty("database");
        Assert.Equal("Unhealthy", databaseEntry.GetProperty("status").GetString());
        Assert.Equal("Database unavailable", databaseEntry.GetProperty("description").GetString());
        Assert.Equal("boom", databaseEntry.GetProperty("error").GetString());
    }
}
