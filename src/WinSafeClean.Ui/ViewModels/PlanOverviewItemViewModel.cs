namespace WinSafeClean.Ui.ViewModels;

public sealed record PlanOverviewItemViewModel(
    string Path,
    string Action,
    string RiskLevel,
    string Reasons,
    string? QuarantinePath,
    string? RestoreMetadataPath);
