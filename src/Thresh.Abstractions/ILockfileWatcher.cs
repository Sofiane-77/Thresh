namespace Thresh.Abstractions;

public interface ILockfileWatcher
{
    /// <summary>Returns current LCU credentials (port/token) if available.</summary>
    Task<LcuCredentials> GetCurrentAsync(CancellationToken ct = default);
}
