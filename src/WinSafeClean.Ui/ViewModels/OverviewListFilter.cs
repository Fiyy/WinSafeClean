namespace WinSafeClean.Ui.ViewModels;

public static class OverviewListFilter
{
    public static IReadOnlyList<ScanReportOverviewItemViewModel> ApplyScanFilter(
        IEnumerable<ScanReportOverviewItemViewModel> items,
        ScanOverviewFilter filter)
    {
        ArgumentNullException.ThrowIfNull(items);

        var query = items.Where(item =>
            MatchesText(filter.SearchText, item.Path, item.Reasons, item.Blockers, item.Evidence)
            && MatchesSelection(filter.RiskLevel, item.RiskLevel)
            && MatchesSelection(filter.ItemKind, item.ItemKind));

        query = filter.Sort switch
        {
            ScanOverviewSort.PathAscending => query.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase),
            ScanOverviewSort.RiskAscending => query
                .OrderBy(item => item.RiskLevel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase),
            ScanOverviewSort.ItemKindAscending => query
                .OrderBy(item => item.ItemKind, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase),
            _ => query
                .OrderByDescending(item => item.SizeBytes)
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
        };

        return query.ToList();
    }

    public static IReadOnlyList<PlanOverviewItemViewModel> ApplyPlanFilter(
        IEnumerable<PlanOverviewItemViewModel> items,
        PlanOverviewFilter filter)
    {
        ArgumentNullException.ThrowIfNull(items);

        var query = items.Where(item =>
            MatchesText(filter.SearchText, item.Path, item.Reasons, item.QuarantinePath, item.RestoreMetadataPath)
            && MatchesSelection(filter.RiskLevel, item.RiskLevel)
            && MatchesSelection(filter.Action, item.Action));

        query = filter.Sort switch
        {
            PlanOverviewSort.ActionAscending => query
                .OrderBy(item => item.Action, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase),
            PlanOverviewSort.RiskAscending => query
                .OrderBy(item => item.RiskLevel, StringComparer.OrdinalIgnoreCase)
                .ThenBy(item => item.Path, StringComparer.OrdinalIgnoreCase),
            _ => query.OrderBy(item => item.Path, StringComparer.OrdinalIgnoreCase)
        };

        return query.ToList();
    }

    public static bool MatchesScanItem(ScanReportOverviewItemViewModel item, ScanOverviewFilter filter)
    {
        return MatchesText(filter.SearchText, item.Path, item.Reasons, item.Blockers, item.Evidence)
            && MatchesSelection(filter.RiskLevel, item.RiskLevel)
            && MatchesSelection(filter.ItemKind, item.ItemKind);
    }

    public static bool MatchesPlanItem(PlanOverviewItemViewModel item, PlanOverviewFilter filter)
    {
        return MatchesText(filter.SearchText, item.Path, item.Reasons, item.QuarantinePath, item.RestoreMetadataPath)
            && MatchesSelection(filter.RiskLevel, item.RiskLevel)
            && MatchesSelection(filter.Action, item.Action);
    }

    private static bool MatchesSelection(string selectedValue, string itemValue)
    {
        return string.IsNullOrWhiteSpace(selectedValue)
            || selectedValue.Equals("All", StringComparison.OrdinalIgnoreCase)
            || itemValue.Equals(selectedValue, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesText(string searchText, params string?[] values)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return values.Any(value =>
            !string.IsNullOrWhiteSpace(value)
            && value.Contains(searchText, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed record ScanOverviewFilter(
    string SearchText,
    string RiskLevel,
    string ItemKind,
    ScanOverviewSort Sort);

public enum ScanOverviewSort
{
    SizeDescending,
    PathAscending,
    RiskAscending,
    ItemKindAscending
}

public sealed record PlanOverviewFilter(
    string SearchText,
    string RiskLevel,
    string Action,
    PlanOverviewSort Sort);

public enum PlanOverviewSort
{
    PathAscending,
    ActionAscending,
    RiskAscending
}
