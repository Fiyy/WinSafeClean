using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Quarantine;

public static class QuarantineExecutionResultJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(QuarantineExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return JsonSerializer.Serialize(result, Options);
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
