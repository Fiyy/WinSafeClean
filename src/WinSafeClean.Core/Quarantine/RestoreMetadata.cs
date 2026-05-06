using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Quarantine;

public sealed record RestoreMetadata(
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    string CleanupPlanSchemaVersion,
    string RestorePlanId,
    string OriginalPath,
    string QuarantinePath,
    string RestoreMetadataPath,
    RiskLevel RiskLevel,
    CleanupPlanAction PlanAction,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Warnings,
    bool RequiresManualConfirmation,
    bool Redacted);
