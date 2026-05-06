using WinSafeClean.Core.Planning;

namespace WinSafeClean.Core.Quarantine;

public static class RestoreMetadataGenerator
{
    private const string CurrentSchemaVersion = "1.0";

    public static IReadOnlyList<RestoreMetadata> Generate(CleanupPlan plan, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(plan);

        return plan.Items
            .Where(item => item.QuarantinePreview is not null)
            .Select(item => CreateMetadata(plan, item, item.QuarantinePreview!, createdAt))
            .ToList();
    }

    private static RestoreMetadata CreateMetadata(
        CleanupPlan plan,
        CleanupPlanItem item,
        QuarantinePreview preview,
        DateTimeOffset createdAt)
    {
        return new RestoreMetadata(
            SchemaVersion: CurrentSchemaVersion,
            CreatedAt: createdAt,
            CleanupPlanSchemaVersion: plan.SchemaVersion,
            RestorePlanId: preview.RestorePlanId,
            OriginalPath: preview.OriginalPath,
            QuarantinePath: preview.ProposedQuarantinePath,
            RestoreMetadataPath: preview.RestoreMetadataPath,
            RiskLevel: item.RiskLevel,
            PlanAction: item.Action,
            Reasons: item.Reasons,
            Warnings: preview.Warnings,
            RequiresManualConfirmation: preview.RequiresManualConfirmation,
            Redacted: false);
    }
}
