using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Quarantine;

public static class QuarantinePreflightChecklistJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(QuarantinePreflightChecklist checklist)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        return JsonSerializer.Serialize(checklist, Options);
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
