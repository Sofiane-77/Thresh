# Quickstart

## Installation
```powershell
dotnet add package Thresh.Extensions
```

## DI & premiers appels
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Thresh.Abstractions;
using Thresh.Extensions;

var services = new ServiceCollection()
    .AddLogging(b => b.AddSimpleConsole())
    .AddThresh(o => {
        o.AcceptSelfSignedCertificates = true;       // HTTP LCU (loopback uniquement)
        o.WsAcceptSelfSignedCertificates = true;     // WebSocket (loopback uniquement)
    });

using var sp = services.BuildServiceProvider();
var api = sp.GetRequiredService<ILcuHttpClient>();
var ws  = sp.GetRequiredService<IEventStream>();

await ws.ConnectAsync();
await ws.WaitUntilConnectedAsync(TimeSpan.FromSeconds(3));

// Snapshot + évènements typés
using var sub = ws.Subscribe<System.Text.Json.JsonElement>("/lol-gameflow/v1/session", data =>
{
    var phase = data.TryGetProperty("phase", out var p) ? p.GetString() : "?";
    Console.WriteLine($"Gameflow phase = {phase}");
}, withSnapshot: true);

// Simple GET
var me = await api.GetAsync<System.Text.Json.JsonElement>("/lol-summoner/v1/current-summoner");
Console.WriteLine(me.GetProperty("displayName").GetString());
```
> Assurez‑vous que le League Client est lancé (ou configurez `LCU_LOCKFILE`).

## Outil DocFX local
Installe l’outil puis build la doc :
```bash
dotnet tool update -g docfx
docfx docs/docfx.json --serve
```
Ouvrez ensuite http://localhost:8080 pour prévisualiser.

La CI `docs.yml` déploie `docs/_site` sur GitHub Pages (Settings → Pages → Source GitHub Actions).
