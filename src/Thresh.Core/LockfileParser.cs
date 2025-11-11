using Thresh.Abstractions;

namespace Thresh.Core;

public static class LockfileParser
{
    /// <summary>
    /// Parse a lockfile line with zero allocations (except final strings for token/protocol).
    /// Format: app:PID:port:password:protocol
    /// </summary>
    public static bool TryParse(ReadOnlySpan<char> line, out LcuCredentials creds)
    {
        creds = null!;

        int c1 = line.IndexOf(':'); if (c1 < 0) return false;
        int c2 = line[(c1 + 1)..].IndexOf(':'); if (c2 < 0) return false; c2 += c1 + 1;
        int c3 = line[(c2 + 1)..].IndexOf(':'); if (c3 < 0) return false; c3 += c2 + 1;
        int c4 = line[(c3 + 1)..].IndexOf(':'); if (c4 < 0) return false; c4 += c3 + 1;

        var portSpan = line[(c2 + 1)..c3];
        if (!int.TryParse(portSpan, out var port)) return false;

        var token = line[(c3 + 1)..c4].ToString();
        var proto = line[(c4 + 1)..].ToString();

        creds = new LcuCredentials(port, token, proto);
        return true;
    }

    public static bool TryReadFile(string path, out LcuCredentials creds)
    {
        creds = null!;
        try
        {
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            using var sr = new StreamReader(fs, System.Text.Encoding.UTF8, true);
            var line = sr.ReadLine();
            return line is not null && TryParse(line.AsSpan(), out creds);
        }
        catch { return false; }
    }
}
