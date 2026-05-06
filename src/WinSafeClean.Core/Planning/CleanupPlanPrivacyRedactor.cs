using System.Text.RegularExpressions;

namespace WinSafeClean.Core.Planning;

public static class CleanupPlanPrivacyRedactor
{
    private static readonly Regex WindowsPathPattern = new(
        @"(?i)(?:[A-Z]:\\[^\s\]\)\}""'<>|]+|\\\\[^\\\s]+\\[^\s\]\)\}""'<>|]+)",
        RegexOptions.Compiled);

    public static CleanupPlan Redact(CleanupPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var aliases = CreateAliases(plan.Items);
        var quarantinePathAliases = CreatePreviewPathAliases(
            plan.Items,
            item => item.QuarantinePreview?.ProposedQuarantinePath,
            "[redacted-quarantine-path");
        var restoreMetadataPathAliases = CreatePreviewPathAliases(
            plan.Items,
            item => item.QuarantinePreview?.RestoreMetadataPath,
            "[redacted-restore-metadata-path");
        var restorePlanIdAliases = CreatePreviewPathAliases(
            plan.Items,
            item => item.QuarantinePreview?.RestorePlanId,
            "[redacted-restore-plan-id");

        return plan with
        {
            QuarantineRoot = string.IsNullOrWhiteSpace(plan.QuarantineRoot)
                ? null
                : "[redacted-quarantine-root]",
            Items = plan.Items
                .Select(item => RedactItem(item, aliases, quarantinePathAliases, restoreMetadataPathAliases, restorePlanIdAliases, plan.QuarantineRoot))
                .ToList()
        };
    }

    private static Dictionary<string, string> CreateAliases(IReadOnlyList<CleanupPlanItem> items)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (!aliases.ContainsKey(item.Path))
            {
                aliases[item.Path] = $"[redacted-path-{aliases.Count + 1:0000}]";
            }
        }

        return aliases;
    }

    private static Dictionary<string, string> CreatePreviewPathAliases(
        IReadOnlyList<CleanupPlanItem> items,
        Func<CleanupPlanItem, string?> valueSelector,
        string prefix)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            var value = valueSelector(item);
            if (string.IsNullOrWhiteSpace(value) || aliases.ContainsKey(value))
            {
                continue;
            }

            aliases[value] = $"{prefix}-{aliases.Count + 1:0000}]";
        }

        return aliases;
    }

    private static CleanupPlanItem RedactItem(
        CleanupPlanItem item,
        Dictionary<string, string> pathAliases,
        Dictionary<string, string> quarantinePathAliases,
        Dictionary<string, string> restoreMetadataPathAliases,
        Dictionary<string, string> restorePlanIdAliases,
        string? quarantineRoot)
    {
        return item with
        {
            Path = pathAliases[item.Path],
            Reasons = item.Reasons
                .Select(reason => RedactKnownValues(reason, pathAliases, quarantinePathAliases, restoreMetadataPathAliases, restorePlanIdAliases, quarantineRoot))
                .ToList(),
            QuarantinePreview = item.QuarantinePreview is null
                ? null
                : RedactPreview(item.QuarantinePreview, pathAliases, quarantinePathAliases, restoreMetadataPathAliases, restorePlanIdAliases, quarantineRoot)
        };
    }

    private static QuarantinePreview RedactPreview(
        QuarantinePreview preview,
        Dictionary<string, string> pathAliases,
        Dictionary<string, string> quarantinePathAliases,
        Dictionary<string, string> restoreMetadataPathAliases,
        Dictionary<string, string> restorePlanIdAliases,
        string? quarantineRoot)
    {
        return preview with
        {
            OriginalPath = RedactKnownValues(preview.OriginalPath, pathAliases, quarantinePathAliases, restoreMetadataPathAliases, restorePlanIdAliases, quarantineRoot),
            ProposedQuarantinePath = quarantinePathAliases[preview.ProposedQuarantinePath],
            RestoreMetadataPath = restoreMetadataPathAliases[preview.RestoreMetadataPath],
            RestorePlanId = restorePlanIdAliases[preview.RestorePlanId],
            Warnings = preview.Warnings
                .Select(warning => RedactKnownValues(warning, pathAliases, quarantinePathAliases, restoreMetadataPathAliases, restorePlanIdAliases, quarantineRoot))
                .ToList()
        };
    }

    private static string RedactKnownValues(
        string value,
        Dictionary<string, string> pathAliases,
        Dictionary<string, string> quarantinePathAliases,
        Dictionary<string, string> restoreMetadataPathAliases,
        Dictionary<string, string> restorePlanIdAliases,
        string? quarantineRoot)
    {
        var aliases = pathAliases
            .Concat(quarantinePathAliases)
            .Concat(restoreMetadataPathAliases)
            .Concat(restorePlanIdAliases)
            .OrderByDescending(alias => alias.Key.Length);

        var redacted = value;
        foreach (var alias in aliases)
        {
            redacted = redacted.Replace(alias.Key, alias.Value, StringComparison.OrdinalIgnoreCase);
        }

        if (!string.IsNullOrWhiteSpace(quarantineRoot))
        {
            redacted = redacted.Replace(quarantineRoot, "[redacted-quarantine-root]", StringComparison.OrdinalIgnoreCase);
        }

        return WindowsPathPattern.Replace(redacted, "[redacted-path]");
    }
}
