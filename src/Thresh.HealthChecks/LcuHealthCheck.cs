using Microsoft.Extensions.Diagnostics.HealthChecks;

using Thresh.Abstractions;

namespace Thresh.HealthChecks;

public sealed class LcuHealthCheck(ILcuHttpClient http) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            // Cheap and reliable endpoint (adjust if needed). We don't need the value, just the success.
            _ = await http.GetAsync<System.Text.Json.JsonElement>("/lol-gameflow/v1/session", ct);
            return HealthCheckResult.Healthy("LCU reachable");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("LCU not reachable", ex);
        }
    }
}
