using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return JsonSerializer.Serialize(report, Options);
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
        };

        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
