namespace WinSafeClean.Ui.ViewModels;

public sealed record ScanReportOverviewItemViewModel(
    string Path,
    string ItemKind,
    long SizeBytes,
    string SizeDisplay,
    string LastWriteTimeDisplay,
    string RiskLevel,
    string SuggestedAction,
    string SpaceUseHint,
    string Reasons,
    string Blockers,
    string Evidence)
{
    public ResultDispositionAdvice DispositionAdvice => ResultDispositionAdvisor.ForScanItem(this);

    public string DispositionTitle => DispositionAdvice.Title;

    public string DispositionMessage => DispositionAdvice.Message;

    public string DispositionNextStep => DispositionAdvice.NextStep;
}
