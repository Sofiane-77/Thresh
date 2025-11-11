using System.Diagnostics;
using System.Runtime.InteropServices;

using Thresh.Abstractions;

namespace Thresh.Core;

/// <summary>
/// Resolves the current League Client (LCU) credentials by locating and parsing the lockfile.
/// Strategy:
///  1) Honors the LCU_LOCKFILE environment variable (absolute path).
///  2) Uses a cached path if previously found and still readable (fast path).
///  3) Falls back to process scanning and known install locations.
///  4) Performs a short active wait to handle client startup.
/// Notes:
///  - We only cache the lockfile *path*, not the token/port, because the file content can change between runs.
///    The file is re-read every time to pick up fresh credentials.
///  - Linux (Wine) users can still set LCU_LOCKFILE explicitly; we also try a common Wine path.
/// </summary>
public sealed class LockfileWatcher : ILockfileWatcher
{
    private string? _cachedLockfilePath; // path cache to avoid repeated process scans

    public async Task<LcuCredentials> GetCurrentAsync(CancellationToken ct = default)
    {
        // 1) Explicit override via environment takes precedence over any cache
        var envPath = Environment.GetEnvironmentVariable("LCU_LOCKFILE");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            if (LockfileParser.TryReadFile(envPath, out var creds))
            {
                _cachedLockfilePath = envPath;
                return creds;
            }
        }

        // 2) Fast path: try previously found path first
        var cached = _cachedLockfilePath;
        if (!string.IsNullOrEmpty(cached) && File.Exists(cached))
        {
            if (LockfileParser.TryReadFile(cached, out var creds))
            {
                return creds;
            }
            // If reading fails (client restarted/moved), clear cache and continue resolution
            _cachedLockfilePath = null;
        }

        // 3) Try to infer from running processes
        foreach (var path in FindCandidatePathsFromProcesses())
        {
            if (LockfileParser.TryReadFile(path, out var creds))
            {
                _cachedLockfilePath = path;
                return creds;
            }
        }

        // 4) Try known install locations (when process hasn't started yet)
        foreach (var path in KnownInstallCandidates())
        {
            if (File.Exists(path) && LockfileParser.TryReadFile(path, out var creds))
            {
                _cachedLockfilePath = path;
                return creds;
            }
        }

        // 5) Short active wait (improves UX during client startup)
        var delay = TimeSpan.FromMilliseconds(250);
        for (var i = 0; i < 20 && !ct.IsCancellationRequested; i++)
        {
            foreach (var path in FindCandidatePathsFromProcesses())
            {
                if (LockfileParser.TryReadFile(path, out var creds))
                {
                    _cachedLockfilePath = path;
                    return creds;
                }
            }

            await Task.Delay(delay, ct);
        }

        throw new InvalidOperationException(
            "LCU lockfile not found. Start the League Client or set LCU_LOCKFILE to the lockfile path.");
    }

    private static IEnumerable<string> FindCandidatePathsFromProcesses()
    {
        // Build a list to avoid 'yield return' inside try/catch (CS1626).
        var results = new List<string>();
        var names = new[] { "LeagueClientUx", "LeagueClientUx.exe", "LeagueClient", "RiotClientServices" };

        foreach (var p in Process.GetProcesses())
        {
            try
            {
                if (!names.Contains(p.ProcessName, StringComparer.OrdinalIgnoreCase))
                {
                    continue;
                }

                var exe = p.MainModule?.FileName; // can throw on some processes → guarded by catch
                if (string.IsNullOrEmpty(exe))
                {
                    continue;
                }

                var dir = Path.GetDirectoryName(exe)!;

                // Windows typical layout: ...\League of Legends\lockfile
                var winLock = Path.Combine(dir, "lockfile");
                if (File.Exists(winLock))
                {
                    results.Add(winLock);
                }

                // macOS app bundle: League of Legends.app/Contents/LoL/lockfile
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    var parent = Directory.GetParent(dir);
                    if (parent is not null)
                    {
                        var macLock = Path.Combine(parent.FullName, "LoL", "lockfile");
                        if (File.Exists(macLock))
                        {
                            results.Add(macLock);
                        }

                        var gparent = parent.Parent;
                        if (gparent is not null)
                        {
                            var macLock2 = Path.Combine(gparent.FullName, "LoL", "lockfile");
                            if (File.Exists(macLock2))
                            {
                                results.Add(macLock2);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Access denied on some processes/modules: ignore and continue.
            }
            finally
            {
                try { p.Dispose(); } catch { /* ignore */ }
            }
        }

        return results;
    }

    private static IEnumerable<string> KnownInstallCandidates()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            yield return @"C:\Riot Games\League of Legends\lockfile";
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                "Riot Games", "League of Legends", "lockfile");
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                "Riot Games", "League of Legends", "lockfile");
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            yield return "/Applications/League of Legends.app/Contents/LoL/lockfile";
            yield return "/Applications/Riot Games/League of Legends.app/Contents/LoL/lockfile";
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // Common Wine path (may vary by setup; users can set LCU_LOCKFILE if needed)
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrEmpty(home))
            {
                yield return Path.Combine(home, ".wine", "drive_c", "Riot Games", "League of Legends", "lockfile");
            }
        }
        // Other setups (e.g., Proton) are not predictable; rely on LCU_LOCKFILE override.
    }
}
