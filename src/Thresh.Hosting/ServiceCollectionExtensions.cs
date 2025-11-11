using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Thresh.Hosting;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Ajoute le hosted service qui gère le cycle de vie du WebSocket.
    /// </summary>
    public static IServiceCollection AddThreshHostedService(this IServiceCollection services)
        => services.AddHostedService<ThreshHostedService>();

    /// <summary>
    /// Variante pour les apps basées sur HostApplicationBuilder.
    /// </summary>
    public static IHostApplicationBuilder AddThreshHosted(this IHostApplicationBuilder builder)
    {
        builder.Services.AddHostedService<ThreshHostedService>();
        return builder;
    }
}
