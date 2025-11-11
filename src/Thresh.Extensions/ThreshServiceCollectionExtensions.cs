using System.Net.Http;
using System.Net.Security; // For SslPolicyErrors

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Polly;

using Thresh.Abstractions;
using Thresh.Core;
using Thresh.Core.Handlers;
using Thresh.Core.Http;
using Thresh.Core.Observability;

namespace Thresh.Extensions;

public static class ThreshServiceCollectionExtensions
{
    public static IServiceCollection AddThresh(this IServiceCollection services, Action<ThreshOptions>? configure = null)
    {
        services.AddOptions<ThreshOptions>();
        if (configure is not null)
        {
            services.PostConfigure(configure);
        }

        services.AddSingleton<ILockfileWatcher, LockfileWatcher>();

        // Observability
        services.AddSingleton<ThreshMetrics>(); // Meter + instruments

        // Handlers
        services.AddTransient<LcuAuthHandler>();
        services.AddTransient<PollyHandler>();

        services.AddSingleton<ResiliencePipeline<HttpResponseMessage>>(sp =>
        {
            var opts = sp.GetRequiredService<IOptions<ThreshOptions>>().Value;
            var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Thresh.Polly");
            return PollyPolicies.CreateHttpPipeline(opts, log);
        });

        // LCU HTTP client
        services.AddHttpClient<ILcuHttpClient, LcuHttpClient>()
            .ConfigureHttpClient((sp, http) =>
            {
                var opts = sp.GetRequiredService<IOptions<ThreshOptions>>().Value;
                http.Timeout = opts.HttpTimeout;
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var opts = sp.GetRequiredService<IOptions<ThreshOptions>>().Value;
                var handler = new HttpClientHandler();
                if (opts.AcceptSelfSignedCertificates)
                {
                    // Accept only specific errors and only on loopback hosts.
                    handler.ServerCertificateCustomValidationCallback = static (msg, cert, chain, errors) =>
                    {
                        var uri = msg?.RequestUri;
                        if (uri is null) return false;
                        var hostOk = uri.Host == "127.0.0.1" || uri.Host == "localhost" || uri.Host == "::1";
                        const SslPolicyErrors allowed =
                            SslPolicyErrors.RemoteCertificateChainErrors |
                            SslPolicyErrors.RemoteCertificateNameMismatch;
                        return hostOk && (errors == SslPolicyErrors.None || (errors & ~allowed) == 0);
                    };
                }
                return handler;
            })
            .AddHttpMessageHandler<LcuAuthHandler>()
            .AddHttpMessageHandler<PollyHandler>();

        // Live Game Client (https://127.0.0.1:2999)
        services.AddTransient<Thresh.Game.GameAuthHandler>();
        services.AddHttpClient<Thresh.Game.IGameHttpClient, Thresh.Game.GameHttpClient>()
            .ConfigureHttpClient(http =>
            {
                http.BaseAddress = new Uri("https://127.0.0.1:2999");
                http.Timeout = TimeSpan.FromSeconds(5);
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new HttpClientHandler();
                // Game client uses a self-signed cert: accept loopback:2999 only.
                handler.ServerCertificateCustomValidationCallback = static (msg, cert, chain, errors) =>
                {
                    var uri = msg?.RequestUri;
                    if (uri is null) return false;
                    var hostOk = uri.Host == "127.0.0.1" || uri.Host == "localhost" || uri.Host == "::1";
                    var portOk = uri.Port == 2999;
                    return hostOk && portOk;
                };
                return handler;
            })
            .AddHttpMessageHandler<Thresh.Game.GameAuthHandler>()
            .AddHttpMessageHandler<PollyHandler>();

        services.AddSingleton<IEventStream, EventStream>();
        return services;
    }
}
