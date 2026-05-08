using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class PlanOverviewViewModelTests
{
    [Fact]
    public void ShouldSummarizeCleanupPlanActionsAndRiskLevels()
    {
        var viewModel = PlanOverviewViewModel.FromPlan(CreatePlan());

        Assert.Equal(3, viewModel.TotalItems);
        Assert.Contains(viewModel.ActionSummaries, item => item.Label == "Keep" && item.Count == 1);
        Assert.Contains(viewModel.ActionSummaries, item => item.Label == "ReportOnly" && item.Count == 1);
        Assert.Contains(viewModel.ActionSummaries, item => item.Label == "ReviewForQuarantine" && item.Count == 1);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "Blocked" && item.Count == 1);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "LowRisk" && item.Count == 1);
    }

    [Fact]
    public void ShouldExposeReasonsAndQuarantinePreviewForPlanItems()
    {
        var viewModel = PlanOverviewViewModel.FromPlan(CreatePlan());

        var item = Assert.Single(viewModel.Items.Where(item => item.Action == "ReviewForQuarantine"));

        Assert.Equal(@"C:\Temp\cache.tmp", item.Path);
        Assert.Equal("LowRisk", item.RiskLevel);
        Assert.Contains("Known cleanup rule matched", item.Reasons);
        Assert.Equal(@"C:\Quarantine\items\abcd-cache.tmp", item.QuarantinePath);
        Assert.Equal(@"C:\Quarantine\restore\abcd.restore.json", item.RestoreMetadataPath);
    }

    [Fact]
    public void EmptyPlanOverviewShouldExposeEmptyState()
    {
        Assert.False(PlanOverviewViewModel.Empty.HasItems);
        Assert.Equal("No cleanup plan loaded.", PlanOverviewViewModel.Empty.EmptyStateMessage);
        Assert.Equal("Select a cleanup plan item to view details.", PlanOverviewViewModel.Empty.SelectionEmptyStateMessage);
    }

    private static CleanupPlan CreatePlan()
    {
        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            QuarantineRoot: @"C:\Quarantine",
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Windows\System32\kernel32.dll",
                    Action: CleanupPlanAction.Keep,
                    RiskLevel: RiskLevel.Blocked,
                    Reasons: ["Protected Windows path."],
                    QuarantinePreview: null),
                new CleanupPlanItem(
                    Path: @"C:\Temp\log.txt",
                    Action: CleanupPlanAction.ReportOnly,
                    RiskLevel: RiskLevel.MediumRisk,
                    Reasons: ["Report only until reviewed."],
                    QuarantinePreview: null),
                new CleanupPlanItem(
                    Path: @"C:\Temp\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Temp\cache.tmp",
                        ProposedQuarantinePath: @"C:\Quarantine\items\abcd-cache.tmp",
                        RestoreMetadataPath: @"C:\Quarantine\restore\abcd.restore.json",
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: ["Quarantine preview only."]))
            ]);
    }
}
