using System.Net.Http;

namespace Thresh.Game;

/// <summary>
/// Force les requêtes vers le Live Game Client (https://127.0.0.1:2999).
/// La validation de certificat est gérée par le HttpClientHandler configuré côté DI.
/// </summary>
public sealed class GameAuthHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
    {
        var uri = request.RequestUri!;
        if (!uri.IsAbsoluteUri)
        {
            request.RequestUri = new Uri(new Uri("https://127.0.0.1:2999"), uri);
        }
        else if (uri.Host != "127.0.0.1" || uri.Port != 2999)
        {
            throw new InvalidOperationException("Game client requests must target https://127.0.0.1:2999");
        }

        return base.SendAsync(request, ct);
    }
}
