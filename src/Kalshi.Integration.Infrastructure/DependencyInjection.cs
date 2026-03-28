using Kalshi.Integration.Application.Abstractions;
using Kalshi.Integration.Infrastructure.Operations;
using Kalshi.Integration.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Kalshi.Integration.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("KalshiIntegration") ?? "Data Source=kalshi-integration-sandbox.db";

        services.AddDbContext<KalshiIntegrationDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<ITradingRepository, EfTradingRepository>();
        services.AddSingleton<IOperationalIssueStore, InMemoryOperationalIssueStore>();
        services.AddSingleton<IAuditRecordStore, InMemoryAuditRecordStore>();
        services.AddSingleton<IIdempotencyStore, InMemoryIdempotencyStore>();
        services.AddSingleton<InMemoryApplicationEventPublisher>();
        services.AddSingleton<IApplicationEventPublisher>(sp => sp.GetRequiredService<InMemoryApplicationEventPublisher>());

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live", "ready"])
            .AddCheck<DatabaseReadinessHealthCheck>("database", tags: ["ready"]);

        return services;
    }
}
