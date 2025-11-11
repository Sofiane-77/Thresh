using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Thresh.Samples.Console;

internal static class Program
{
    static async Task<int> Main(string[] args)
    {
        await using var provider = Bootstrap.BuildServiceProvider();
        var log = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Sample");

        log.LogInformation("Thresh sample runner started. Args: {Args}", string.Join(' ', args));

        var demos = new (string Name, Func<IServiceProvider, CancellationToken, Task> Run)[]
        {
            ("http-get", Demos.HttpGetAsync),
            ("ws-connect", Demos.WsConnectAsync),
            ("sub-uri", Demos.SubscribeUriAsync),
            ("sub-regex", Demos.SubscribeRegexAsync),
            ("sub-typed-snapshot", Demos.SubscribeTypedSnapshotAsync),
            ("endpoints", Demos.EndpointsAsync),
            ("reactive", Demos.ReactiveAsync),
            ("domain", Demos.DomainAsync),
            ("game-get", Demos.GameGetAsync),
        };

        using var cts = new CancellationTokenSource();
        System.Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

        if (args.Length == 0 && !System.Console.IsInputRedirected)
            return await InteractiveMenu(provider, demos, cts.Token, log);

        if (args.Length == 0)
        {
            System.Console.WriteLine("Usage: dotnet run -- [demo]");
            System.Console.WriteLine("Available demos:");
            foreach (var d in demos) System.Console.WriteLine($"  - {d.Name}");
            System.Console.WriteLine("Or run all: dotnet run -- all");
            return 0;
        }

        if (args[0].Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            foreach (var d in demos)
            {
                log.LogInformation("=== Running demo: {Demo} === (press Ctrl+C to stop)", d.Name);
                await d.Run(provider, cts.Token);
                if (cts.IsCancellationRequested) break;
            }
            return 0;
        }

        var selected = demos.FirstOrDefault(d => d.Name.Equals(args[0], StringComparison.OrdinalIgnoreCase));
        if (selected.Run is null)
        {
            log.LogError("Unknown demo '{Arg}'.", args[0]);
            System.Console.WriteLine("Available demos:");
            foreach (var d in demos) System.Console.WriteLine($"  - {d.Name}");
            return 1;
        }

        log.LogInformation("=== Running demo: {Demo} === (press Ctrl+C to stop)", selected.Name);
        await selected.Run(provider, cts.Token);
        return 0;
    }

    private static async Task<int> InteractiveMenu(
        IServiceProvider sp,
        (string Name, Func<IServiceProvider, CancellationToken, Task> Run)[] demos,
        CancellationToken ct,
        ILogger log)
    {
        while (!ct.IsCancellationRequested)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Select a demo to run:");
            for (int i = 0; i < demos.Length; i++)
                System.Console.WriteLine($"  {i + 1}) {demos[i].Name}");
            System.Console.WriteLine("  a) all");
            System.Console.WriteLine("  q) quit");
            System.Console.Write("> ");

            var input = System.Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(input)) continue;

            if (input.Equals("q", StringComparison.OrdinalIgnoreCase))
                return 0;

            if (input.Equals("a", StringComparison.OrdinalIgnoreCase) || input.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var d in demos)
                {
                    log.LogInformation("=== Running demo: {Demo} === (press Ctrl+C to stop)", d.Name);
                    await d.Run(sp, ct);
                    if (ct.IsCancellationRequested) break;
                }
                return 0;
            }

            var tokens = input.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var list = new List<(string Name, Func<IServiceProvider, CancellationToken, Task> Run)>();
            foreach (var t in tokens)
            {
                if (int.TryParse(t, out var idx) && idx >= 1 && idx <= demos.Length)
                {
                    list.Add(demos[idx - 1]);
                    continue;
                }
                var byName = demos.FirstOrDefault(d => d.Name.Equals(t, StringComparison.OrdinalIgnoreCase));
                if (byName.Run is not null) list.Add(byName);
                else System.Console.WriteLine($"Unknown selection: '{t}'");
            }

            if (list.Count == 0) continue;
            foreach (var d in list)
            {
                log.LogInformation("=== Running demo: {Demo} === (press Ctrl+C to stop)", d.Name);
                await d.Run(sp, ct);
                if (ct.IsCancellationRequested) break;
            }
            return 0;
        }
        return 0;
    }
}
