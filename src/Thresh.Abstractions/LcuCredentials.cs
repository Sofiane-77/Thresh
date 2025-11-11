namespace Thresh.Abstractions;

public sealed record LcuCredentials(int Port, string Token, string Protocol = "https")
{
    public Uri BaseAddress => new($"{Protocol}://127.0.0.1:{Port}");
    public string BasicAuthValue => Convert.ToBase64String(
        System.Text.Encoding.ASCII.GetBytes($"riot:{Token}"));
}
