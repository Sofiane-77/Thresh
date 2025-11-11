# Overview

Thresh expose deux surfaces API :
1. **REST** via `ILcuHttpClient`  
2. **Events** via `IEventStream` (WAMP `OnJsonApiEvent`) avec souscriptions par **URI** ou **Regex**, et souscriptions **typées** (snapshot initial optionnel).

## Principes
- Async-only, CancellationToken partout, DI-first
- Résilience (retry/circuit) via Polly v8
- Observabilité : `ILogger`, `Meter` (dotnet-counters) et `ActivitySource("Thresh")` pour OpenTelemetry
- Sécurité : certificats auto-signés **acceptés uniquement en loopback**
