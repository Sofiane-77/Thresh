using Microsoft.Extensions.DependencyInjection;

using Thresh.Abstractions;
using Thresh.Extensions;

using Xunit;

namespace Thresh.Core.Tests;

public class WebSocketResolvingTests
{
    [Fact]
    public void ServiceProvider_resolves_EventStream()
    {
        var services = new ServiceCollection().AddLogging().AddThresh();
        using var sp = services.BuildServiceProvider();
        Assert.NotNull(sp.GetRequiredService<IEventStream>());
    }
}
