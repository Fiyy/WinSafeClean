namespace WinSafeClean.Ui.ViewModels;

public sealed record PlanOverviewItemViewModel(
    string Path,
    string Action,
    string RiskLevel,
    string Reasons,
    string? QuarantinePath,
    string? RestoreMetadataPath)
{
    public ResultDispositionAdvice DispositionAdvice => ResultDispositionAdvisor.ForPlanItem(this);

    public string DispositionTitle => DispositionAdvice.Title;

    public string DispositionMessage => DispositionAdvice.Message;

    public string DispositionNextStep => DispositionAdvice.NextStep;

    public bool CanPreparePreflight => DispositionAdvice.CanPreparePreflight;
}
