using Microsoft.Extensions.DependencyInjection;

using Thresh.Abstractions;
using Thresh.Extensions;

using Xunit;

namespace Thresh.Core.Tests;

public class SmokeTests
{
    [Fact]
    public void ServiceProvider_resolves_LcuHttpClient()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddThresh();

        using var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<ILcuHttpClient>();
        Assert.NotNull(client);
    }
}
