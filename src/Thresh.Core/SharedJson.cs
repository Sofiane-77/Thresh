using System.Text.Json;

namespace Thresh.Core;

// EN: Centralize JsonSerializerOptions to avoid allocations and ensure consistent behavior.
internal static class SharedJson
{
    public static readonly JsonSerializerOptions Options =
        new(JsonSerializerDefaults.Web) { PropertyNameCaseInsensitive = true };
}
