namespace WinSafeClean.Ui.ViewModels;

public sealed record ScanReportOverviewItemViewModel(
    string Path,
    string ItemKind,
    long SizeBytes,
    string SizeDisplay,
    string LastWriteTimeDisplay,
    string RiskLevel,
    string SuggestedAction,
    string Reasons,
    string Blockers,
    string Evidence);
