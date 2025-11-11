using Thresh.Endpoints.Models;

namespace Thresh.Endpoints;

public interface ILolSummonerApi
{
    Task<SummonerDto?> GetCurrentAsync(CancellationToken ct = default);
    Task<SummonerDto?> GetByPuuidAsync(string puuid, CancellationToken ct = default);
}
