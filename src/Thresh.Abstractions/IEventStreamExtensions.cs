using System;
using System.Threading;
using System.Threading.Tasks;

namespace Thresh.Abstractions;

// EN: Ergonomic helper without breaking the IEventStream interface.
//     Allows: await stream.ConnectAndWaitAsync(TimeSpan.FromSeconds(3));
public static class IEventStreamExtensions
{
    public static async Task<bool> ConnectAndWaitAsync(this IEventStream stream, TimeSpan timeout, CancellationToken ct = default)
    {
        await stream.ConnectAsync(ct);
        return await stream.WaitUntilConnectedAsync(timeout, ct);
    }
}
