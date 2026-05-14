using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class ResultDispositionAdvisorTests
{
    [Fact]
    public void ForScanItem_ShouldKeepBlockedItemsAndAvoidManualCleanup()
    {
        var item = CreateScanItem(riskLevel: "Blocked", suggestedAction: "Keep");

        var advice = ResultDispositionAdvisor.ForScanItem(item);

        Assert.Equal("Keep this item", advice.Title);
        Assert.Contains("blocked", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not manually clean", advice.NextStep, StringComparison.Ordinal);
        Assert.False(advice.CanPreparePreflight);
    }

    [Fact]
    public void ForScanItem_ShouldRouteReportOnlyItemsToPlanReview()
    {
        var item = CreateScanItem(riskLevel: "LowRisk", suggestedAction: "ReportOnly");

        var advice = ResultDispositionAdvisor.ForScanItem(item);

        Assert.Equal("Review in Plan", advice.Title);
        Assert.Contains("scan report is evidence", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Run Plan", advice.NextStep, StringComparison.Ordinal);
        Assert.False(advice.CanPreparePreflight);
    }

    [Fact]
    public void ForPlanItem_ShouldKeepBlockedItemsInPlace()
    {
        var item = CreatePlanItem(action: "Keep", riskLevel: "Blocked", quarantinePath: null, restoreMetadataPath: null);

        var advice = ResultDispositionAdvisor.ForPlanItem(item);

        Assert.Equal("Keep this item", advice.Title);
        Assert.Contains("should stay in place", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Do not build", advice.NextStep, StringComparison.Ordinal);
        Assert.False(advice.CanPreparePreflight);
    }

    [Fact]
    public void ForPlanItem_ShouldExplainReportOnlyHasNoFileMove()
    {
        var item = CreatePlanItem(action: "ReportOnly", riskLevel: "MediumRisk", quarantinePath: null, restoreMetadataPath: null);

        var advice = ResultDispositionAdvisor.ForPlanItem(item);

        Assert.Equal("Report only", advice.Title);
        Assert.Contains("no file-moving action", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("review evidence", advice.NextStep, StringComparison.OrdinalIgnoreCase);
        Assert.False(advice.CanPreparePreflight);
    }

    [Fact]
    public void ForPlanItem_ShouldAllowPreflightForQuarantineCandidatesWithPreview()
    {
        var item = CreatePlanItem(
            action: "ReviewForQuarantine",
            riskLevel: "LowRisk",
            quarantinePath: @"C:\Quarantine\items\cache.tmp",
            restoreMetadataPath: @"C:\Quarantine\restore\cache.restore.json");

        var advice = ResultDispositionAdvisor.ForPlanItem(item);

        Assert.Equal("Prepare Safety Check", advice.Title);
        Assert.Contains("candidate", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Run Safety Check", advice.NextStep, StringComparison.Ordinal);
        Assert.True(advice.CanPreparePreflight);
    }

    [Fact]
    public void ForPlanItem_ShouldBlockPreflightWhenQuarantinePreviewIsMissing()
    {
        var item = CreatePlanItem(
            action: "ReviewForQuarantine",
            riskLevel: "LowRisk",
            quarantinePath: null,
            restoreMetadataPath: null);

        var advice = ResultDispositionAdvisor.ForPlanItem(item);

        Assert.Equal("Missing quarantine preview", advice.Title);
        Assert.Contains("cannot prepare preflight", advice.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(advice.CanPreparePreflight);
    }

    private static ScanReportOverviewItemViewModel CreateScanItem(string riskLevel, string suggestedAction)
    {
        return new ScanReportOverviewItemViewModel(
            Path: @"C:\Temp\cache.tmp",
            ItemKind: "File",
            SizeBytes: 1024,
            SizeDisplay: "1 KB",
            LastWriteTimeDisplay: "-",
            RiskLevel: riskLevel,
            SuggestedAction: suggestedAction,
            SpaceUseHint: "Temporary-looking location.",
            Reasons: "Test reason.",
            Blockers: string.Empty,
            Evidence: string.Empty);
    }

    private static PlanOverviewItemViewModel CreatePlanItem(
        string action,
        string riskLevel,
        string? quarantinePath,
        string? restoreMetadataPath)
    {
        return new PlanOverviewItemViewModel(
            Path: @"C:\Temp\cache.tmp",
            Action: action,
            RiskLevel: riskLevel,
            Reasons: "Test reason.",
            QuarantinePath: quarantinePath,
            RestoreMetadataPath: restoreMetadataPath);
    }
}
