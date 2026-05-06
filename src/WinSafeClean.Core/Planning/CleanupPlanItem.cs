using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Planning;

public sealed record CleanupPlanItem(
    string Path,
    CleanupPlanAction Action,
    RiskLevel RiskLevel,
    IReadOnlyList<string> Reasons,
    QuarantinePreview? QuarantinePreview = null);
