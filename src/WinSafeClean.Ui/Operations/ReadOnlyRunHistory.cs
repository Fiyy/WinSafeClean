using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Ui.Operations;

public static class ReadOnlyRunHistory
{
    public static IReadOnlyList<ReadOnlyRunHistoryEntry> Add(
        IEnumerable<ReadOnlyRunHistoryEntry> entries,
        ReadOnlyRunHistoryEntry entry,
        int maxEntries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(entry);

        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Maximum entry count must be positive.");
        }

        var normalizedEntry = entry with
        {
            TargetPath = NormalizePath(entry.TargetPath),
            OutputPath = NormalizePath(entry.OutputPath),
            Format = NormalizeFormat(entry.Format)
        };

        return entries
            .Append(normalizedEntry)
            .OrderByDescending(candidate => candidate.CompletedAt)
            .ThenBy(candidate => candidate.KindLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.OutputPath, StringComparer.OrdinalIgnoreCase)
            .Take(maxEntries)
            .ToList();
    }

    private static string NormalizePath(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string trimmed = Environment.ExpandEnvironmentVariables(path.Trim());

        try
        {
            return Path.GetFullPath(trimmed);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return trimmed;
        }
    }

    private static string NormalizeFormat(string format)
    {
        return string.IsNullOrWhiteSpace(format)
            ? "json"
            : format.Trim().ToLowerInvariant();
    }
}

public sealed class ReadOnlyRunHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly int _maxEntries;

    public ReadOnlyRunHistoryStore(string path, int maxEntries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Maximum entry count must be positive.");
        }

        _path = path;
        _maxEntries = maxEntries;
    }

    public static ReadOnlyRunHistoryStore CreateDefault()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string root = string.IsNullOrWhiteSpace(localApplicationData)
            ? Path.GetTempPath()
            : localApplicationData;
        return new ReadOnlyRunHistoryStore(
            Path.Combine(root, "WinSafeClean", "read-only-run-history.json"),
            maxEntries: 50);
    }

    public IReadOnlyList<ReadOnlyRunHistoryEntry> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<ReadOnlyRunHistoryEntry>>(
                File.ReadAllText(_path),
                JsonOptions);

            return entries is null
                ? []
                : entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.TargetPath)
                        && !string.IsNullOrWhiteSpace(entry.OutputPath))
                    .OrderByDescending(entry => entry.CompletedAt)
                    .ThenBy(entry => entry.KindLabel, StringComparer.OrdinalIgnoreCase)
                    .Take(_maxEntries)
                    .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return [];
        }
    }

    public IReadOnlyList<ReadOnlyRunHistoryEntry> Add(ReadOnlyRunHistoryEntry entry)
    {
        var entries = ReadOnlyRunHistory.Add(Load(), entry, _maxEntries);
        Save(entries);
        return entries;
    }

    public void Clear()
    {
        Save([]);
    }

    private void Save(IReadOnlyList<ReadOnlyRunHistoryEntry> entries)
    {
        string? parent = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(entries, JsonOptions));
    }
}

public sealed record ReadOnlyRunHistoryEntry(
    ReadOnlyRunHistoryKind Kind,
    string TargetPath,
    string OutputPath,
    string Format,
    int ExitCode,
    DateTimeOffset StartedAt,
    DateTimeOffset CompletedAt)
{
    [JsonIgnore]
    public bool Succeeded => ExitCode == 0;

    [JsonIgnore]
    public bool CanOpenInUi => Succeeded && Format.Equals("json", StringComparison.OrdinalIgnoreCase);

    [JsonIgnore]
    public string KindLabel => Kind switch
    {
        ReadOnlyRunHistoryKind.Scan => "Evidence Scan",
        ReadOnlyRunHistoryKind.Plan => "Cleanup Plan",
        ReadOnlyRunHistoryKind.SafetyCheck => "Safety Check",
        _ => Kind.ToString()
    };

    [JsonIgnore]
    public string StatusLabel => Succeeded ? "Succeeded" : "Failed";

    [JsonIgnore]
    public string CompletedAtDisplay => CompletedAt.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");

    [JsonIgnore]
    public string DisplayText => $"{CompletedAtDisplay} - {KindLabel} - {StatusLabel} - {TargetPath}";
}

public enum ReadOnlyRunHistoryKind
{
    Scan,
    Plan,
    SafetyCheck
}
