using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Quarantine;

public static class QuarantineOperationLogJsonLinesSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string SerializeEntry(QuarantineOperationLogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return JsonSerializer.Serialize(entry, Options) + Environment.NewLine;
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            WriteIndented = false
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
