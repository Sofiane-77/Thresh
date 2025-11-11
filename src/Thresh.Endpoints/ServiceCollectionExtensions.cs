using Microsoft.Extensions.DependencyInjection;

namespace Thresh.Endpoints;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddThreshEndpoints(this IServiceCollection services)
    {
        services.AddSingleton<ILolSummonerApi, LolSummonerApi>();
        // Add other endpoint groups here (Gameflow, Champ Select, etc.)
        return services;
    }
}
