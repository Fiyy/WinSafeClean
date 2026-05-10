using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;

namespace WinSafeClean.Ui.Operations;

public static class PlanPreflightPreparation
{
    public static RestoreMetadata CreateRestoreMetadataForPlanItem(
        CleanupPlan plan,
        string planItemPath,
        string? restoreMetadataPath,
        DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentException.ThrowIfNullOrWhiteSpace(planItemPath);

        if (string.IsNullOrWhiteSpace(restoreMetadataPath))
        {
            throw new InvalidOperationException("Selected plan item does not have a quarantine preview.");
        }

        var metadata = RestoreMetadataGenerator.Generate(plan, createdAt)
            .FirstOrDefault(item =>
                item.OriginalPath.Equals(planItemPath, StringComparison.OrdinalIgnoreCase)
                && item.RestoreMetadataPath.Equals(restoreMetadataPath, StringComparison.OrdinalIgnoreCase));

        return metadata
            ?? throw new InvalidOperationException("Selected plan item was not found in the loaded cleanup plan.");
    }
}
