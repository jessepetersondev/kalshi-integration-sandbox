using Kalshi.Integration.Application.Dashboard;
using Kalshi.Integration.Application.Operations;
using Kalshi.Integration.Application.Risk;
using Kalshi.Integration.Application.Trading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kalshi.Integration.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RiskOptions>(configuration.GetSection(RiskOptions.SectionName));
        services.AddScoped<RiskEvaluator>();
        services.AddScoped<IdempotencyService>();
        services.AddScoped<TradingService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
