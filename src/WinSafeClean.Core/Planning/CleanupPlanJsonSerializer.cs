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

    public static CleanupPlan Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<CleanupPlan>(json, Options)
            ?? throw new InvalidOperationException("Cleanup plan JSON did not contain a plan.");
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = true
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
