using FsCheck;
using FsCheck.Xunit;

using Thresh.Core;

namespace Thresh.Core.Tests;

public class LockfileParserPropertyTests
{
    [Property(MaxTest = 100)]
    public void Parser_accepts_valid_formats_and_rejects_invalid(int port, string token)
    {
        port = Math.Abs(port % 65535);
        if (port is 0)
        {
            port = 2999;
        }

        token ??= "abc";

        var line = $"LeagueClientUx:12345:{port}:{token}:https";
        var ok = LockfileParser.TryParse(line, out var creds);
        Assert.True(ok, "valid line should parse");
        Assert.True(creds.Port == port, "port should match");
        Assert.True(creds.Token == token, "token should match");

        var bad = "bad:line";
        var nok = LockfileParser.TryParse(bad, out _);
        Assert.False(nok, "invalid line should not parse");
    }
}
