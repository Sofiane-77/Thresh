using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using Thresh.Abstractions;
using Thresh.Extensions;

using Xunit;

namespace Thresh.Core.Tests;

public class WebSocketDispatchTests
{
    [Fact]
    public void Can_resolve_and_subscribe()
    {
        var sp = new ServiceCollection().AddLogging().AddThresh().BuildServiceProvider();
        var stream = sp.GetRequiredService<IEventStream>();
        using var sub = stream.Subscribe("/lol-test/v1/foo", _ => { /* no-op */ });
        Assert.NotNull(sub);
    }
}
