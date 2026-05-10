using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class OverviewListFilterTests
{
    [Fact]
    public void ApplyScanFilter_SearchesPathReasonsAndEvidence()
    {
        var items = new[]
        {
            CreateScanItem(@"C:\Temp\cache.bin", 10, "SafeCandidate", "File", reasons: "Known cache rule."),
            CreateScanItem(@"C:\Logs\app.log", 20, "LowRisk", "File", evidence: "Service: Example - references app")
        };

        var filtered = OverviewListFilter.ApplyScanFilter(
            items,
            new ScanOverviewFilter(SearchText: "service", RiskLevel: "All", ItemKind: "All", Sort: ScanOverviewSort.SizeDescending));

        Assert.Single(filtered);
        Assert.Equal(@"C:\Logs\app.log", filtered[0].Path);
    }

    [Fact]
    public void ApplyScanFilter_FiltersByRiskAndKind()
    {
        var items = new[]
        {
            CreateScanItem(@"C:\Temp\cache.bin", 10, "SafeCandidate", "File"),
            CreateScanItem(@"C:\Temp\cache", 30, "SafeCandidate", "Directory"),
            CreateScanItem(@"C:\Windows\System32", 0, "Blocked", "Directory")
        };

        var filtered = OverviewListFilter.ApplyScanFilter(
            items,
            new ScanOverviewFilter(SearchText: "", RiskLevel: "SafeCandidate", ItemKind: "Directory", Sort: ScanOverviewSort.SizeDescending));

        Assert.Single(filtered);
        Assert.Equal(@"C:\Temp\cache", filtered[0].Path);
    }

    [Fact]
    public void ApplyScanFilter_SortsBySizeDescendingThenPath()
    {
        var items = new[]
        {
            CreateScanItem(@"C:\B", 10, "LowRisk", "File"),
            CreateScanItem(@"C:\A", 10, "LowRisk", "File"),
            CreateScanItem(@"C:\C", 30, "LowRisk", "File")
        };

        var filtered = OverviewListFilter.ApplyScanFilter(
            items,
            new ScanOverviewFilter(SearchText: "", RiskLevel: "All", ItemKind: "All", Sort: ScanOverviewSort.SizeDescending));

        Assert.Equal([@"C:\C", @"C:\A", @"C:\B"], filtered.Select(item => item.Path));
    }

    [Fact]
    public void ApplyPlanFilter_SearchesPathAndReasons()
    {
        var items = new[]
        {
            CreatePlanItem(@"C:\Temp\cache.bin", "ReviewForQuarantine", "SafeCandidate", "Known cache rule."),
            CreatePlanItem(@"C:\Tools\keep.dll", "Keep", "HighRisk", "Referenced by shortcut.")
        };

        var filtered = OverviewListFilter.ApplyPlanFilter(
            items,
            new PlanOverviewFilter(SearchText: "shortcut", RiskLevel: "All", Action: "All", Sort: PlanOverviewSort.PathAscending));

        Assert.Single(filtered);
        Assert.Equal(@"C:\Tools\keep.dll", filtered[0].Path);
    }

    [Fact]
    public void ApplyPlanFilter_FiltersByRiskAndAction()
    {
        var items = new[]
        {
            CreatePlanItem(@"C:\Temp\cache.bin", "ReviewForQuarantine", "SafeCandidate", "Known cache rule."),
            CreatePlanItem(@"C:\Tools\keep.dll", "Keep", "HighRisk", "Referenced by shortcut.")
        };

        var filtered = OverviewListFilter.ApplyPlanFilter(
            items,
            new PlanOverviewFilter(SearchText: "", RiskLevel: "SafeCandidate", Action: "ReviewForQuarantine", Sort: PlanOverviewSort.PathAscending));

        Assert.Single(filtered);
        Assert.Equal(@"C:\Temp\cache.bin", filtered[0].Path);
    }

    [Fact]
    public void ApplyPlanFilter_SortsByActionThenPath()
    {
        var items = new[]
        {
            CreatePlanItem(@"C:\Z", "ReviewForQuarantine", "SafeCandidate", "Known cache rule."),
            CreatePlanItem(@"C:\B", "Keep", "HighRisk", "Referenced."),
            CreatePlanItem(@"C:\A", "Keep", "HighRisk", "Referenced.")
        };

        var filtered = OverviewListFilter.ApplyPlanFilter(
            items,
            new PlanOverviewFilter(SearchText: "", RiskLevel: "All", Action: "All", Sort: PlanOverviewSort.ActionAscending));

        Assert.Equal([@"C:\A", @"C:\B", @"C:\Z"], filtered.Select(item => item.Path));
    }

    private static ScanReportOverviewItemViewModel CreateScanItem(
        string path,
        long sizeBytes,
        string risk,
        string kind,
        string reasons = "",
        string evidence = "")
    {
        return new ScanReportOverviewItemViewModel(
            Path: path,
            ItemKind: kind,
            SizeBytes: sizeBytes,
            SizeDisplay: sizeBytes.ToString(),
            LastWriteTimeDisplay: "-",
            RiskLevel: risk,
            SuggestedAction: "ReportOnly",
            SpaceUseHint: "",
            Reasons: reasons,
            Blockers: "",
            Evidence: evidence);
    }

    private static PlanOverviewItemViewModel CreatePlanItem(
        string path,
        string action,
        string risk,
        string reasons)
    {
        return new PlanOverviewItemViewModel(
            Path: path,
            Action: action,
            RiskLevel: risk,
            Reasons: reasons,
            QuarantinePath: null,
            RestoreMetadataPath: null);
    }
}
