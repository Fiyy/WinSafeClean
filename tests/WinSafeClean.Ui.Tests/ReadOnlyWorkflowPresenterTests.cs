using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class ReadOnlyWorkflowPresenterTests
{
    [Fact]
    public void Create_DisablesPrimaryActionUntilScanTargetExists()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: false,
            ScanCompleted: false,
            PlanCompleted: false,
            PreflightInputsReady: false,
            PreflightCompleted: false));

        Assert.Equal(ReadOnlyWorkflowAction.RunScan, view.PrimaryAction);
        Assert.Equal("Run Evidence Scan", view.PrimaryActionText);
        Assert.False(view.PrimaryActionEnabled);
        Assert.Equal("Needs target", view.ScanStatus);
        Assert.Equal("Pending", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
        Assert.Equal("Choose review target", view.CurrentStepTitle);
        Assert.Equal("Pick a Quick Start location or choose a folder/file before running the evidence scan.", view.CurrentStepDetail);
        Assert.Equal("No delete or clean command is available from this UI.", view.SafetyBoundary);
    }

    [Fact]
    public void Create_StartsWithRunScanWhenTargetExists()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: true,
            ScanCompleted: false,
            PlanCompleted: false,
            PreflightInputsReady: false,
            PreflightCompleted: false));

        Assert.Equal(ReadOnlyWorkflowAction.RunScan, view.PrimaryAction);
        Assert.Equal("Run Evidence Scan", view.PrimaryActionText);
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Ready", view.ScanStatus);
        Assert.Equal("Pending", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
        Assert.Equal("Run evidence scan", view.CurrentStepTitle);
        Assert.Equal("Create a JSON scan report so the review has size, risk and evidence data.", view.CurrentStepDetail);
    }

    [Fact]
    public void Create_MovesToRunPlanAfterScanCompletes()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: true,
            ScanCompleted: true,
            PlanCompleted: false,
            PreflightInputsReady: false,
            PreflightCompleted: false));

        Assert.Equal(ReadOnlyWorkflowAction.RunPlan, view.PrimaryAction);
        Assert.Equal("Create Plan", view.PrimaryActionText);
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.ScanStatus);
        Assert.Equal("Ready", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
        Assert.Equal("Create cleanup plan", view.CurrentStepTitle);
        Assert.Equal("Use the same target to classify each item as keep, report-only or review for quarantine.", view.CurrentStepDetail);
    }

    [Fact]
    public void Create_MovesToPlanReviewAfterPlanLoads()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: true,
            ScanCompleted: true,
            PlanCompleted: true,
            PreflightInputsReady: false,
            PreflightCompleted: false));

        Assert.Equal(ReadOnlyWorkflowAction.ReviewPlan, view.PrimaryAction);
        Assert.Equal("Review Plan", view.PrimaryActionText);
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.ScanStatus);
        Assert.Equal("Done", view.PlanStatus);
        Assert.Equal("Needs candidate", view.PreflightStatus);
        Assert.Equal("Review plan result", view.CurrentStepTitle);
        Assert.Equal("Select a ReviewForQuarantine candidate only if its evidence and risk level are acceptable.", view.CurrentStepDetail);
    }

    [Fact]
    public void Create_MovesToRunPreflightWhenInputsAreReady()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: true,
            ScanCompleted: true,
            PlanCompleted: true,
            PreflightInputsReady: true,
            PreflightCompleted: false));

        Assert.Equal(ReadOnlyWorkflowAction.RunPreflight, view.PrimaryAction);
        Assert.Equal("Run Safety Check", view.PrimaryActionText);
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Ready", view.PreflightStatus);
        Assert.Equal("Run safety check", view.CurrentStepTitle);
        Assert.Equal("Validate the selected candidate and restore metadata before building any file-moving command.", view.CurrentStepDetail);
    }

    [Fact]
    public void Create_EndsAtPreflightReview()
    {
        var view = ReadOnlyWorkflowPresenter.Create(new ReadOnlyWorkflowSnapshot(
            HasScanTarget: true,
            ScanCompleted: true,
            PlanCompleted: true,
            PreflightInputsReady: true,
            PreflightCompleted: true));

        Assert.Equal(ReadOnlyWorkflowAction.ReviewPreflight, view.PrimaryAction);
        Assert.Equal("Review Safety Check", view.PrimaryActionText);
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.PreflightStatus);
        Assert.Equal("Review safety check", view.CurrentStepTitle);
        Assert.Equal("Only build a guarded CLI handoff if the checklist result is acceptable.", view.CurrentStepDetail);
    }
}
