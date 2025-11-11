# Thresh — .NET Library for the League Client (LCU)

**Thresh** est une librairie .NET 8 pour l’API **League Client (LCU)** orientée craft :
- Async-first, DI-first, gestion systématique des `CancellationToken`
- HTTP via `HttpClientFactory` + **Polly v8** (retry + circuit-breaker)
- WebSocket WAMP (`OnJsonApiEvent`) avec souscriptions **URI/Regex** et **snapshot initial** optionnel
- Observabilité : `ILogger`, métriques (`Meter`) et **ActivitySource "Thresh"** (OTel‑ready)
- API propre, tests, samples et docs DocFX

## Installation
```powershell
dotnet add package Thresh.Extensions
```

## Démarrage rapide
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Thresh.Abstractions;
using Thresh.Extensions;

var services = new ServiceCollection()
    .AddLogging(b => b.AddSimpleConsole())
    .AddThresh(o =>
    {
        o.AcceptSelfSignedCertificates = true;     // HTTP LCU (loopback uniquement)
        o.WsAcceptSelfSignedCertificates = true;  // WebSocket (loopback uniquement)
    });

using var sp = services.BuildServiceProvider();
var api = sp.GetRequiredService<ILcuHttpClient>();
var ws  = sp.GetRequiredService<IEventStream>();

await ws.ConnectAsync();
await ws.WaitUntilConnectedAsync(TimeSpan.FromSeconds(3));

// Souscription typée + snapshot
using var sub = ws.Subscribe<System.Text.Json.JsonElement>("/lol-gameflow/v1/session", data =>
{
    var phase = data.TryGetProperty("phase", out var p) ? p.GetString() : "?";
    Console.WriteLine($"Gameflow phase = {phase}");
}, withSnapshot: true);

// GET simple
var me = await api.GetAsync<System.Text.Json.JsonElement>("/lol-summoner/v1/current-summoner");
Console.WriteLine(me.GetProperty("displayName").GetString());
```

> Assurez‑vous que le **League Client** est lancé (ou configurez `LCU_LOCKFILE` vers le `lockfile`).

## Structure
- `src/` — bibliothèques de prod
- `tests/` — tests xUnit
- `samples/` — exemples exécutables
- `docs/` — DocFX (API + guides)

## Docs locales (DocFX)
```bash
dotnet tool update -g docfx
docfx docs/docfx.json --serve
# Puis ouvrez http://localhost:8080
```

## Licence
MIT — voir [LICENSE](LICENSE).
