using Microsoft.Extensions.DependencyInjection;

namespace Thresh.HealthChecks;

public static class ServiceCollectionExtensions
{
    public static IHealthChecksBuilder AddThreshHealthChecks(this IServiceCollection services)
        => services.AddHealthChecks().AddCheck<LcuHealthCheck>("lcu");
}
