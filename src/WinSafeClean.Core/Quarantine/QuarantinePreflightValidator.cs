using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Quarantine;

public static class QuarantinePreflightValidator
{
    private const string CurrentSchemaVersion = "1.0";

    public static QuarantinePreflightChecklist Validate(
        CleanupPlan plan,
        RestoreMetadata metadata,
        DateTimeOffset createdAt,
        bool manualConfirmationProvided)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(metadata);

        var checks = new List<QuarantinePreflightCheck>();
        var planItem = FindPlanItem(plan, metadata);
        var preview = planItem?.QuarantinePreview;
        var normalizedQuarantineRoot = ValidateQuarantineRoot(plan, checks);

        AddCheck(
            checks,
            "CleanupPlanSchemaSupported",
            plan.SchemaVersion == "0.2" && metadata.CleanupPlanSchemaVersion == "0.2",
            "Cleanup plan and restore metadata schema versions are supported.",
            "Unsupported cleanup plan or restore metadata source schema version.");

        AddCheck(
            checks,
            "RestoreMetadataNotRedacted",
            !metadata.Redacted,
            "Restore metadata is full fidelity and can be used for execution.",
            "Redacted restore metadata cannot be used for execution.");

        AddCheck(
            checks,
            "PathsAreFullFidelity",
            PathsAreFullFidelity(plan, metadata),
            "Preflight paths and restore plan id are full fidelity.",
            "Redacted path tokens or restore plan ids cannot be used for execution.");

        AddCheck(
            checks,
            "PlanItemMatchesMetadata",
            PlanItemMatchesMetadata(planItem, preview, metadata),
            "Restore metadata matches the cleanup plan quarantine preview.",
            "Restore metadata does not match any cleanup plan quarantine preview.");

        AddCheck(
            checks,
            "ActionAllowsQuarantine",
            metadata.PlanAction == CleanupPlanAction.ReviewForQuarantine
                && planItem?.Action == CleanupPlanAction.ReviewForQuarantine,
            "Plan action is ReviewForQuarantine.",
            "Only ReviewForQuarantine items can pass quarantine preflight.");

        AddCheck(
            checks,
            "PreviewRequiresManualConfirmation",
            preview?.RequiresManualConfirmation == true && metadata.RequiresManualConfirmation,
            "Quarantine preview and restore metadata require manual confirmation.",
            "Quarantine preview and restore metadata must require manual confirmation.");

        AddCheck(
            checks,
            "RiskAllowsQuarantine",
            metadata.RiskLevel is RiskLevel.LowRisk or RiskLevel.SafeCandidate,
            "Risk level allows quarantine review.",
            "Blocked, high-risk, medium-risk, or unknown-risk items cannot pass quarantine preflight.");

        AddCheck(
            checks,
            "ManualConfirmation",
            !metadata.RequiresManualConfirmation || manualConfirmationProvided,
            "Manual confirmation was provided.",
            "Manual confirmation is required before quarantine execution.");

        AddCheck(
            checks,
            "PathsRemainInsideQuarantineRoot",
            normalizedQuarantineRoot is not null
                && IsSamePathOrChild(metadata.QuarantinePath, normalizedQuarantineRoot)
                && IsSamePathOrChild(metadata.RestoreMetadataPath, normalizedQuarantineRoot),
            "Quarantine and restore metadata paths remain under the quarantine root.",
            "Quarantine or restore metadata path escapes the quarantine root.");

        AddCheck(
            checks,
            "SourceOutsideQuarantineRoot",
            normalizedQuarantineRoot is not null
                && !IsSamePathOrChild(metadata.OriginalPath, normalizedQuarantineRoot),
            "Source path is outside the quarantine root.",
            "Source path is already inside the quarantine root.");

        AddCheck(
            checks,
            "NoSourceTargetOverlap",
            !PathsEqual(metadata.OriginalPath, metadata.QuarantinePath)
                && !PathsEqual(metadata.OriginalPath, metadata.RestoreMetadataPath),
            "Source, quarantine target, and restore metadata paths are distinct.",
            "Source path must not equal quarantine target or restore metadata path.");

        AddCheck(
            checks,
            "NoTargetMetadataOverlap",
            !PathsEqual(metadata.QuarantinePath, metadata.RestoreMetadataPath),
            "Quarantine target and restore metadata paths are distinct.",
            "Quarantine target path must not equal restore metadata path.");

        return new QuarantinePreflightChecklist(
            SchemaVersion: CurrentSchemaVersion,
            CreatedAt: createdAt,
            IsExecutable: checks.All(check => check.Status != QuarantinePreflightCheckStatus.Failed),
            Checks: checks);
    }

    private static CleanupPlanItem? FindPlanItem(CleanupPlan plan, RestoreMetadata metadata)
    {
        return plan.Items.FirstOrDefault(item =>
            item.QuarantinePreview?.RestorePlanId.Equals(metadata.RestorePlanId, StringComparison.OrdinalIgnoreCase) == true);
    }

    private static string? ValidateQuarantineRoot(CleanupPlan plan, List<QuarantinePreflightCheck> checks)
    {
        if (string.IsNullOrWhiteSpace(plan.QuarantineRoot))
        {
            checks.Add(new QuarantinePreflightCheck(
                Code: "QuarantineRootAllowed",
                Status: QuarantinePreflightCheckStatus.Failed,
                Message: "Cleanup plan does not include a quarantine root."));
            return null;
        }

        try
        {
            var normalizedRoot = QuarantinePathPlanner.NormalizeQuarantineRoot(plan.QuarantineRoot);
            AddCheck(
                checks,
                "QuarantineRootAllowed",
                PathRiskClassifier.Assess(normalizedRoot).Level != RiskLevel.Blocked,
                "Quarantine root is not a protected Windows path.",
                "Quarantine root must not target a protected Windows path.");
            return normalizedRoot;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            checks.Add(new QuarantinePreflightCheck(
                Code: "QuarantineRootAllowed",
                Status: QuarantinePreflightCheckStatus.Failed,
                Message: "Quarantine root is invalid or protected."));
            return null;
        }
    }

    private static bool PlanItemMatchesMetadata(
        CleanupPlanItem? planItem,
        QuarantinePreview? preview,
        RestoreMetadata metadata)
    {
        return planItem is not null
            && preview is not null
            && planItem.Path.Equals(metadata.OriginalPath, StringComparison.OrdinalIgnoreCase)
            && preview.OriginalPath.Equals(metadata.OriginalPath, StringComparison.OrdinalIgnoreCase)
            && preview.ProposedQuarantinePath.Equals(metadata.QuarantinePath, StringComparison.OrdinalIgnoreCase)
            && preview.RestoreMetadataPath.Equals(metadata.RestoreMetadataPath, StringComparison.OrdinalIgnoreCase)
            && preview.RestorePlanId.Equals(metadata.RestorePlanId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool PathsAreFullFidelity(CleanupPlan plan, RestoreMetadata metadata)
    {
        return !ContainsRedactedToken(plan.QuarantineRoot)
            && !ContainsRedactedToken(metadata.RestorePlanId)
            && !ContainsRedactedToken(metadata.OriginalPath)
            && !ContainsRedactedToken(metadata.QuarantinePath)
            && !ContainsRedactedToken(metadata.RestoreMetadataPath);
    }

    private static bool ContainsRedactedToken(string? value)
    {
        return value?.Contains("[redacted-", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void AddCheck(
        List<QuarantinePreflightCheck> checks,
        string code,
        bool passed,
        string passedMessage,
        string failedMessage)
    {
        checks.Add(new QuarantinePreflightCheck(
            Code: code,
            Status: passed ? QuarantinePreflightCheckStatus.Passed : QuarantinePreflightCheckStatus.Failed,
            Message: passed ? passedMessage : failedMessage));
    }

    private static bool IsSamePathOrChild(string path, string parentPath)
    {
        try
        {
            var normalizedPath = NormalizePathForComparison(path);
            var normalizedParent = NormalizePathForComparison(parentPath);

            return normalizedPath.Equals(normalizedParent, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(normalizedParent + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(normalizedParent + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static bool PathsEqual(string left, string right)
    {
        try
        {
            return NormalizePathForComparison(left).Equals(NormalizePathForComparison(right), StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string NormalizePathForComparison(string path)
    {
        return Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
