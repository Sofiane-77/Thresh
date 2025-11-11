using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Thresh.Game;

public interface IGameHttpClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
}
