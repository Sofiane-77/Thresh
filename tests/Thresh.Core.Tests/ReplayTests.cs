using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Thresh.Abstractions;

using Xunit;

namespace Thresh.Core.Tests;

public class ReplayTests
{
    [Fact]
    public void Replay_envelopes_dispatch_to_subscribers()
    {
        var services = new ServiceCollection().AddLogging(b => b.AddDebug()).BuildServiceProvider();
        var stream = new FakeEventStream(services.GetRequiredService<ILoggerFactory>());

        string? phase = null;
        int updates = 0;

        using var sub1 = stream.Subscribe("/lol-gameflow/v1/session", (LeagueEventEnvelope env) =>
        {
            updates++;
        });

        using var sub2 = stream.Subscribe<JsonElement>("/lol-gameflow/v1/session", data =>
        {
            if (data.TryGetProperty("phase", out var p))
            {
                phase = p.GetString();
            }
        });

        // load the JSONL file (copied to output)
        var path = Path.Combine(AppContext.BaseDirectory, "events.envelopes.jsonl");
        Assert.True(File.Exists(path), "events.envelopes.jsonl missing");

        foreach (var line in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var env = JsonSerializer.Deserialize<LeagueEventEnvelope>(line, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
            stream.Publish(env);
        }

        Assert.True(updates > 0);
        Assert.False(string.IsNullOrEmpty(phase));
    }
}
