# Thresh

**Thresh** est une librairie .NET 8 pour le **League Client (LCU)** :
- HTTP via `HttpClientFactory` avec **Polly** (retry + circuit-breaker)
- WebSocket WAMP (`OnJsonApiEvent`) avec **URI/Regex** et **snapshot initial**
- **DI-first**, `ILogger`, `Meter`, `ActivitySource("Thresh")`
- Modèles `System.Text.Json` (immutables), hiérarchie d’exceptions

👉 Démarrer avec le [Quickstart](articles/quickstart.md) ou parcourir l’[API](api/index.html).
