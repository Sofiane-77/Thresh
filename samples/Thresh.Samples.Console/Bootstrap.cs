using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Thresh.Extensions;
using Thresh.Endpoints;
using Thresh.Domain;

namespace Thresh.Samples.Console;

internal static class Bootstrap
{
    public static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection()
            .AddLogging(b => b.AddSimpleConsole(o => { o.SingleLine = true; o.TimestampFormat = "HH:mm:ss "; }))
            .AddThresh(o =>
            {
                o.AcceptSelfSignedCertificates = true;
                o.HttpTimeout = TimeSpan.FromSeconds(10);
            })
            .AddThreshEndpoints()
            .AddThreshDomain();

        return services.BuildServiceProvider();
    }
}
