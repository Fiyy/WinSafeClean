using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Ui.ViewModels;

public sealed class ScanReportOverviewViewModel
{
    private ScanReportOverviewViewModel(
        string schemaVersion,
        int totalItems,
        IReadOnlyList<SummaryItemViewModel> riskSummaries,
        IReadOnlyList<SummaryItemViewModel> itemKindSummaries,
        IReadOnlyList<ScanReportOverviewItemViewModel> items)
    {
        SchemaVersion = schemaVersion;
        TotalItems = totalItems;
        RiskSummaries = riskSummaries;
        ItemKindSummaries = itemKindSummaries;
        Items = items;
    }

    public string SchemaVersion { get; }

    public int TotalItems { get; }

    public IReadOnlyList<SummaryItemViewModel> RiskSummaries { get; }

    public IReadOnlyList<SummaryItemViewModel> ItemKindSummaries { get; }

    public IReadOnlyList<ScanReportOverviewItemViewModel> Items { get; }

    public static ScanReportOverviewViewModel Empty { get; } = new("n/a", 0, [], [], []);

    public static ScanReportOverviewViewModel FromReport(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var items = report.Items
            .Select(item => new ScanReportOverviewItemViewModel(
                Path: item.Path,
                ItemKind: item.ItemKind.ToString(),
                SizeBytes: item.SizeBytes,
                RiskLevel: item.Risk.Level.ToString(),
                SuggestedAction: item.Risk.SuggestedAction.ToString(),
                Reasons: string.Join(Environment.NewLine, item.Risk.Reasons),
                Blockers: string.Join(Environment.NewLine, item.Risk.Blockers),
                Evidence: string.Join(
                    Environment.NewLine,
                    item.Evidence.Select(evidence => $"{evidence.Type}: {evidence.Source} - {evidence.Message}"))))
            .ToList();

        return new ScanReportOverviewViewModel(
            schemaVersion: report.SchemaVersion,
            totalItems: items.Count,
            riskSummaries: CreateSummary(items.Select(item => item.RiskLevel)),
            itemKindSummaries: CreateSummary(items.Select(item => item.ItemKind)),
            items: items);
    }

    private static IReadOnlyList<SummaryItemViewModel> CreateSummary(IEnumerable<string> values)
    {
        return values
            .GroupBy(value => value, StringComparer.Ordinal)
            .OrderBy(group => group.Key, StringComparer.Ordinal)
            .Select(group => new SummaryItemViewModel(group.Key, group.Count()))
            .ToList();
    }
}
