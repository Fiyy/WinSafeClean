using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Quarantine;

public static class RestoreMetadataJsonSerializer
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Serialize(RestoreMetadata metadata)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        return JsonSerializer.Serialize(metadata, Options);
    }

    public static RestoreMetadata Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<RestoreMetadata>(json, Options)
            ?? throw new InvalidOperationException("Restore metadata JSON did not contain metadata.");
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
