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
        Assert.False(view.PrimaryActionEnabled);
        Assert.Equal("Needs target", view.ScanStatus);
        Assert.Equal("Pending", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
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
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Ready", view.ScanStatus);
        Assert.Equal("Pending", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
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
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.ScanStatus);
        Assert.Equal("Ready", view.PlanStatus);
        Assert.Equal("Pending", view.PreflightStatus);
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
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.ScanStatus);
        Assert.Equal("Done", view.PlanStatus);
        Assert.Equal("Needs candidate", view.PreflightStatus);
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
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Ready", view.PreflightStatus);
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
        Assert.True(view.PrimaryActionEnabled);
        Assert.Equal("Done", view.PreflightStatus);
    }
}
