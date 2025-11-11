using System.Net.Http;

namespace Thresh.Abstractions;

public interface ILcuHttpClient
{
    Task<T?> GetAsync<T>(string path, CancellationToken ct = default);
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct = default);
}
