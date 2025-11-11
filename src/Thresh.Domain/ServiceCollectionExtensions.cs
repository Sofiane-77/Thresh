using Microsoft.Extensions.DependencyInjection;

using Thresh.Domain.Gameflow;

namespace Thresh.Domain;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThreshDomain(this IServiceCollection services)
    {
        services.AddSingleton<IGameflowService, GameflowService>();
        return services;
    }
}
