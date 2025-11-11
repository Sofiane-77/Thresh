using System.Text.Json.Serialization;

namespace Thresh.Endpoints.Models;

public sealed class SummonerDto
{
    [JsonPropertyName("displayName")] public string? DisplayName { get; init; }
    [JsonPropertyName("puuid")] public string? Puuid { get; init; }
    [JsonPropertyName("summonerId")] public long? SummonerId { get; init; }
    [JsonPropertyName("accountId")] public long? AccountId { get; init; }
    [JsonPropertyName("xpSinceLastLevel")] public long? XpSinceLastLevel { get; init; }
}
