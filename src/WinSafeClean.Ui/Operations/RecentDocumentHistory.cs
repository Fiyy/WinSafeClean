using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinSafeClean.Ui.Operations;

public static class RecentDocumentHistory
{
    public static IReadOnlyList<RecentDocumentEntry> Add(
        IEnumerable<RecentDocumentEntry> entries,
        RecentDocumentKind kind,
        string path,
        DateTimeOffset timestamp,
        int maxEntries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Maximum entry count must be positive.");
        }

        string normalizedPath = NormalizePath(path);
        var updatedEntry = new RecentDocumentEntry(kind, normalizedPath, timestamp);

        return entries
            .Where(entry => entry.Kind != kind
                || !NormalizePath(entry.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            .Prepend(updatedEntry)
            .OrderByDescending(entry => entry.LastOpenedAt)
            .ThenBy(entry => entry.DisplayText, StringComparer.OrdinalIgnoreCase)
            .Take(maxEntries)
            .ToList();
    }

    public static IReadOnlyList<RecentDocumentEntry> Remove(
        IEnumerable<RecentDocumentEntry> entries,
        RecentDocumentEntry entryToRemove)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(entryToRemove);

        string normalizedPath = NormalizePath(entryToRemove.Path);
        return entries
            .Where(entry => entry.Kind != entryToRemove.Kind
                || !NormalizePath(entry.Path).Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private static string NormalizePath(string path)
    {
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
}

public sealed class RecentDocumentHistoryStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _path;
    private readonly int _maxEntries;

    public RecentDocumentHistoryStore(string path, int maxEntries)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries), "Maximum entry count must be positive.");
        }

        _path = path;
        _maxEntries = maxEntries;
    }

    public static RecentDocumentHistoryStore CreateDefault()
    {
        string localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string root = string.IsNullOrWhiteSpace(localApplicationData)
            ? Path.GetTempPath()
            : localApplicationData;
        return new RecentDocumentHistoryStore(
            Path.Combine(root, "WinSafeClean", "recent-documents.json"),
            maxEntries: 12);
    }

    public IReadOnlyList<RecentDocumentEntry> Load()
    {
        if (!File.Exists(_path))
        {
            return [];
        }

        try
        {
            var entries = JsonSerializer.Deserialize<List<RecentDocumentEntry>>(
                File.ReadAllText(_path),
                JsonOptions);

            return entries is null
                ? []
                : entries
                    .Where(entry => !string.IsNullOrWhiteSpace(entry.Path))
                    .OrderByDescending(entry => entry.LastOpenedAt)
                    .ThenBy(entry => entry.DisplayText, StringComparer.OrdinalIgnoreCase)
                    .Take(_maxEntries)
                    .ToList();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException or NotSupportedException)
        {
            return [];
        }
    }

    public IReadOnlyList<RecentDocumentEntry> Add(
        RecentDocumentKind kind,
        string path,
        DateTimeOffset timestamp)
    {
        var entries = RecentDocumentHistory.Add(Load(), kind, path, timestamp, _maxEntries);
        Save(entries);
        return entries;
    }

    public IReadOnlyList<RecentDocumentEntry> Remove(RecentDocumentEntry entry)
    {
        var entries = RecentDocumentHistory.Remove(Load(), entry);
        Save(entries);
        return entries;
    }

    public void Clear()
    {
        Save([]);
    }

    private void Save(IReadOnlyList<RecentDocumentEntry> entries)
    {
        string? parent = Path.GetDirectoryName(_path);
        if (!string.IsNullOrWhiteSpace(parent))
        {
            Directory.CreateDirectory(parent);
        }

        File.WriteAllText(_path, JsonSerializer.Serialize(entries, JsonOptions));
    }
}

public sealed record RecentDocumentEntry(
    RecentDocumentKind Kind,
    string Path,
    DateTimeOffset LastOpenedAt)
{
    [JsonIgnore]
    public string DisplayText => $"{GetKindLabel(Kind)} - {Path}";

    private static string GetKindLabel(RecentDocumentKind kind)
    {
        return kind switch
        {
            RecentDocumentKind.ScanReport => "Scan",
            RecentDocumentKind.CleanupPlan => "Plan",
            RecentDocumentKind.PreflightChecklist => "Preflight",
            _ => kind.ToString()
        };
    }
}

public enum RecentDocumentKind
{
    ScanReport,
    CleanupPlan,
    PreflightChecklist
}
