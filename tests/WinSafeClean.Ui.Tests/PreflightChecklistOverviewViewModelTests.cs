using WinSafeClean.Core.Quarantine;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class PreflightChecklistOverviewViewModelTests
{
    [Fact]
    public void ShouldSummarizePreflightChecklistStatuses()
    {
        var viewModel = PreflightChecklistOverviewViewModel.FromChecklist(CreateChecklist());

        Assert.False(viewModel.IsExecutable);
        Assert.Equal(2, viewModel.TotalChecks);
        Assert.Contains(viewModel.StatusSummaries, item => item.Label == "Passed" && item.Count == 1);
        Assert.Contains(viewModel.StatusSummaries, item => item.Label == "Failed" && item.Count == 1);
    }

    [Fact]
    public void ShouldExposeCheckCodeStatusAndMessage()
    {
        var viewModel = PreflightChecklistOverviewViewModel.FromChecklist(CreateChecklist());

        var item = Assert.Single(viewModel.Checks.Where(check => check.Code == "ManualConfirmation"));

        Assert.Equal("Failed", item.Status);
        Assert.Contains("required", item.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void EmptyPreflightOverviewShouldExposeEmptyState()
    {
        Assert.False(PreflightChecklistOverviewViewModel.Empty.HasChecks);
        Assert.Equal("No safety checklist loaded.", PreflightChecklistOverviewViewModel.Empty.EmptyStateMessage);
    }

    private static QuarantinePreflightChecklist CreateChecklist()
    {
        return new QuarantinePreflightChecklist(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            IsExecutable: false,
            Checks:
            [
                new QuarantinePreflightCheck(
                    Code: "RestoreMetadataNotRedacted",
                    Status: QuarantinePreflightCheckStatus.Passed,
                    Message: "Restore metadata is full fidelity."),
                new QuarantinePreflightCheck(
                    Code: "ManualConfirmation",
                    Status: QuarantinePreflightCheckStatus.Failed,
                    Message: "Manual confirmation is required before quarantine execution.")
            ]);
    }
}
