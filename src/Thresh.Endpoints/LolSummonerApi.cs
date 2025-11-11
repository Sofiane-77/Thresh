using Thresh.Abstractions;
using Thresh.Endpoints.Models;

namespace Thresh.Endpoints;

public sealed class LolSummonerApi(ILcuHttpClient http) : ILolSummonerApi
{
    public Task<SummonerDto?> GetCurrentAsync(CancellationToken ct = default)
        => http.GetAsync<SummonerDto>("/lol-summoner/v1/current-summoner", ct);

    public Task<SummonerDto?> GetByPuuidAsync(string puuid, CancellationToken ct = default)
        => http.GetAsync<SummonerDto>($"/lol-summoner/v2/summoners/puuid/{Uri.EscapeDataString(puuid)}", ct);
}
