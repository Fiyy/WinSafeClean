using WinSafeClean.Core.Planning;

namespace WinSafeClean.Ui.ViewModels;

public sealed class PlanOverviewViewModel
{
    private PlanOverviewViewModel(
        int totalItems,
        IReadOnlyList<SummaryItemViewModel> actionSummaries,
        IReadOnlyList<SummaryItemViewModel> riskSummaries,
        IReadOnlyList<PlanOverviewItemViewModel> items)
    {
        TotalItems = totalItems;
        ActionSummaries = actionSummaries;
        RiskSummaries = riskSummaries;
        Items = items;
    }

    public int TotalItems { get; }

    public bool HasItems => TotalItems > 0;

    public string EmptyStateMessage => "No cleanup plan loaded.";

    public IReadOnlyList<SummaryItemViewModel> ActionSummaries { get; }

    public IReadOnlyList<SummaryItemViewModel> RiskSummaries { get; }

    public IReadOnlyList<PlanOverviewItemViewModel> Items { get; }

    public static PlanOverviewViewModel Empty { get; } = new(0, [], [], []);

    public static PlanOverviewViewModel FromPlan(CleanupPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var items = plan.Items
            .Select(item => new PlanOverviewItemViewModel(
                Path: item.Path,
                Action: item.Action.ToString(),
                RiskLevel: item.RiskLevel.ToString(),
                Reasons: string.Join(Environment.NewLine, item.Reasons),
                QuarantinePath: item.QuarantinePreview?.ProposedQuarantinePath,
                RestoreMetadataPath: item.QuarantinePreview?.RestoreMetadataPath))
            .ToList();

        return new PlanOverviewViewModel(
            totalItems: items.Count,
            actionSummaries: CreateSummary(items.Select(item => item.Action)),
            riskSummaries: CreateSummary(items.Select(item => item.RiskLevel)),
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
