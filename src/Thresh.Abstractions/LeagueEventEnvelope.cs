using System.Text.Json;

namespace Thresh.Abstractions;

/// <summary>Standard envelope for an LCU OnJsonApiEvent.</summary>
public sealed class LeagueEventEnvelope
{
    /// <summary>Resource path, e.g. "/lol-summoner/v1/current-summoner".</summary>
    public required string Uri { get; init; }

    /// <summary>Event type: "Create" | "Update" | "Delete".</summary>
    public required string EventType { get; init; }

    /// <summary>Raw JSON payload of the event.</summary>
    public required JsonElement Data { get; init; }
}
