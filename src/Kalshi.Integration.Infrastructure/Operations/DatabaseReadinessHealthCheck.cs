using Kalshi.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Kalshi.Integration.Infrastructure.Operations;

public sealed class DatabaseReadinessHealthCheck : IHealthCheck
{
    private readonly KalshiIntegrationDbContext _dbContext;
    private readonly ILogger<DatabaseReadinessHealthCheck> _logger;

    public DatabaseReadinessHealthCheck(KalshiIntegrationDbContext dbContext, ILogger<DatabaseReadinessHealthCheck> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            stopwatch.Stop();

            _logger.LogInformation(
                "Dependency check {Dependency} {Operation} completed with canConnect={CanConnect} in {ElapsedMs} ms.",
                "sqlite",
                "database.readiness",
                canConnect,
                stopwatch.Elapsed.TotalMilliseconds);

            return canConnect
                ? HealthCheckResult.Healthy("SQLite connectivity verified.")
                : HealthCheckResult.Unhealthy("SQLite connectivity check failed.");
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            _logger.LogError(
                exception,
                "Dependency check {Dependency} {Operation} failed after {ElapsedMs} ms.",
                "sqlite",
                "database.readiness",
                stopwatch.Elapsed.TotalMilliseconds);

            return HealthCheckResult.Unhealthy("SQLite readiness check threw an exception.", exception);
        }
    }
}
