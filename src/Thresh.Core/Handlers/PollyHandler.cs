using System.Net.Http;

using Polly;

namespace Thresh.Core.Handlers;

public sealed class PollyHandler : DelegatingHandler
{
    private readonly ResiliencePipeline<HttpResponseMessage> _pipeline;

    public PollyHandler(ResiliencePipeline<HttpResponseMessage> pipeline)
        => _pipeline = pipeline;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        => _pipeline.ExecuteAsync(
               // Polly v8 expects ValueTask<T>
               (ctx, token) => new ValueTask<HttpResponseMessage>(base.SendAsync(request, token)),
               ct)
           .AsTask(); // Convert ValueTask to Task
}
