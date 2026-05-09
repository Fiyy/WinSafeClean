using System.IO;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Ui.ViewModels;

public sealed class ScanReportOverviewViewModel
{
    private ScanReportOverviewViewModel(
        string schemaVersion,
        int totalItems,
        long totalSizeBytes,
        IReadOnlyList<SummaryItemViewModel> riskSummaries,
        IReadOnlyList<SummaryItemViewModel> itemKindSummaries,
        IReadOnlyList<ScanReportOverviewItemViewModel> items,
        IReadOnlyList<ScanReportOverviewItemViewModel> largestItems,
        IReadOnlyList<ScanReportOverviewItemViewModel> largestDirectories)
    {
        SchemaVersion = schemaVersion;
        TotalItems = totalItems;
        TotalSizeBytes = totalSizeBytes;
        RiskSummaries = riskSummaries;
        ItemKindSummaries = itemKindSummaries;
        Items = items;
        LargestItems = largestItems;
        LargestDirectories = largestDirectories;
    }

    public string SchemaVersion { get; }

    public int TotalItems { get; }

    public long TotalSizeBytes { get; }

    public string TotalSizeDisplay => ByteSizeFormatter.Format(TotalSizeBytes);

    public bool HasItems => TotalItems > 0;

    public string EmptyStateMessage => "No scan report loaded.";

    public string SelectionEmptyStateMessage => "Select a scan item to view details.";

    public IReadOnlyList<SummaryItemViewModel> RiskSummaries { get; }

    public IReadOnlyList<SummaryItemViewModel> ItemKindSummaries { get; }

    public IReadOnlyList<ScanReportOverviewItemViewModel> Items { get; }

    public IReadOnlyList<ScanReportOverviewItemViewModel> LargestItems { get; }

    public bool HasLargestItems => LargestItems.Count > 0;

    public IReadOnlyList<ScanReportOverviewItemViewModel> LargestDirectories { get; }

    public bool HasLargestDirectories => LargestDirectories.Count > 0;

    public static ScanReportOverviewViewModel Empty { get; } = new("n/a", 0, 0, [], [], [], [], []);

    public static ScanReportOverviewViewModel FromReport(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var items = report.Items
            .Select(item => new ScanReportOverviewItemViewModel(
                Path: item.Path,
                ItemKind: item.ItemKind.ToString(),
                SizeBytes: item.SizeBytes,
                SizeDisplay: ByteSizeFormatter.Format(item.SizeBytes),
                LastWriteTimeDisplay: UtcTimestampFormatter.Format(item.LastWriteTimeUtc),
                RiskLevel: item.Risk.Level.ToString(),
                SuggestedAction: item.Risk.SuggestedAction.ToString(),
                SpaceUseHint: CreateSpaceUseHint(item),
                Reasons: string.Join(Environment.NewLine, item.Risk.Reasons),
                Blockers: string.Join(Environment.NewLine, item.Risk.Blockers),
                Evidence: string.Join(
                    Environment.NewLine,
                    item.Evidence.Select(evidence => $"{evidence.Type}: {evidence.Source} - {evidence.Message}"))))
            .OrderByDescending(item => item.SizeBytes)
            .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var largestItems = items
            .Where(item => item.SizeBytes > 0)
            .Take(5)
            .ToList();
        var largestDirectories = items
            .Where(item => item.ItemKind.Equals(ScanReportItemKind.Directory.ToString(), StringComparison.Ordinal)
                && item.SizeBytes > 0)
            .Take(5)
            .ToList();

        return new ScanReportOverviewViewModel(
            schemaVersion: report.SchemaVersion,
            totalItems: items.Count,
            totalSizeBytes: items.Sum(item => item.SizeBytes),
            riskSummaries: CreateSummary(items.Select(item => item.RiskLevel)),
            itemKindSummaries: CreateSummary(items.Select(item => item.ItemKind)),
            items: items,
            largestItems: largestItems,
            largestDirectories: largestDirectories);
    }

    private static IReadOnlyList<SummaryItemViewModel> CreateSummary(IEnumerable<string> values)
    {
        return values
            .GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new SummaryItemViewModel(group.Key, group.Count()))
            .ToList();
    }

    private static string CreateSpaceUseHint(ScanReportItem item)
    {
        if (item.Risk.Level.ToString().Equals("Blocked", StringComparison.Ordinal))
        {
            return "Protected Windows area. Do not clean manually; use Windows-supported tools when applicable.";
        }

        var path = item.Path;
        if (ContainsSegment(path, "Temp") || ContainsSegment(path, "Tmp"))
        {
            return "Temporary-looking location. Review owner, age, and evidence before cleanup.";
        }

        if (ContainsSegment(path, "Cache") || ContainsSegment(path, "Caches"))
        {
            return "Cache-like location. Often regenerable, but verify application ownership and active references.";
        }

        if (ContainsSegment(path, "Logs") || path.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
        {
            return "Log-like item. It may be useful for troubleshooting; review age and owner before cleanup.";
        }

        if (path.EndsWith(".dmp", StringComparison.OrdinalIgnoreCase)
            || ContainsSegment(path, "CrashDumps"))
        {
            return "Crash dump-like item. It may be useful for diagnostics; review before cleanup.";
        }

        if (ContainsSegment(path, "Downloads"))
        {
            return "User download location. Confirm the file is no longer needed before cleanup.";
        }

        return "No common space-use pattern detected. Review evidence and risk before taking action.";
    }

    private static bool ContainsSegment(string path, string segment)
    {
        return path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Any(part => part.Equals(segment, StringComparison.OrdinalIgnoreCase));
    }
}
