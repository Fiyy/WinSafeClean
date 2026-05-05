using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Planning;

public static class CleanupPlanJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(CleanupPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return JsonSerializer.Serialize(plan, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
