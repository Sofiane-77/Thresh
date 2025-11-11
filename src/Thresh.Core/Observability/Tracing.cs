using System.Diagnostics;

namespace Thresh.Core.Observability;

public static class ThreshTracing
{
    public const string ActivitySourceName = "Thresh";
    public static readonly ActivitySource Source = new(ActivitySourceName);
}
