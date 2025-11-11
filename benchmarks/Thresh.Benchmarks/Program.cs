using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

using Microsoft.Extensions.DependencyInjection;

using Thresh.Abstractions;
using Thresh.Extensions;

BenchmarkRunner.Run<HttpBench>();

[MemoryDiagnoser]
public class HttpBench
{
    private ILcuHttpClient _api = default!;

    [GlobalSetup]
    public void Setup()
    {
        var sp = new ServiceCollection()
            .AddLogging()
            .AddThresh()
            .BuildServiceProvider();
        _api = sp.GetRequiredService<ILcuHttpClient>();
    }

    [Benchmark(Description = "GET /lol-gameflow/v1/session (when client is up)")]
    public async Task GetGameflowSession()
    {
        try { _ = await _api.GetAsync<System.Text.Json.JsonElement>("/lol-gameflow/v1/session"); }
        catch { /* ignore if client not running */ }
    }
}
