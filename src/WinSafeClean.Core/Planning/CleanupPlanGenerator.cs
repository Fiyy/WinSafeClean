using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Planning;

public static class CleanupPlanGenerator
{
    private const string CurrentSchemaVersion = "0.2";

    private static readonly HashSet<EvidenceType> ActiveReferenceEvidenceTypes =
    [
        EvidenceType.ServiceReference,
        EvidenceType.ScheduledTaskReference,
        EvidenceType.StartupReference,
        EvidenceType.UninstallRegistryReference,
        EvidenceType.RunningProcessReference,
        EvidenceType.PathEnvironmentReference,
        EvidenceType.ShortcutReference,
        EvidenceType.FileAssociationReference,
        EvidenceType.InstalledApplication,
        EvidenceType.MicrosoftStorePackage
    ];

    public static CleanupPlan Generate(ScanReport report, DateTimeOffset createdAt, string? quarantineRoot = null)
    {
        ArgumentNullException.ThrowIfNull(report);

        var effectiveQuarantineRoot = string.IsNullOrWhiteSpace(quarantineRoot)
            ? QuarantinePathPlanner.GetDefaultQuarantineRoot()
            : QuarantinePathPlanner.NormalizeQuarantineRoot(quarantineRoot);

        return new CleanupPlan(
            SchemaVersion: CurrentSchemaVersion,
            CreatedAt: createdAt,
            Items: report.Items.Select(item => CreatePlanItem(item, effectiveQuarantineRoot)).ToArray(),
            QuarantineRoot: effectiveQuarantineRoot);
    }

    private static CleanupPlanItem CreatePlanItem(ScanReportItem reportItem, string quarantineRoot)
    {
        var reasons = new List<string>();
        var action = ChooseAction(reportItem, reasons);
        var quarantinePreview = action == CleanupPlanAction.ReviewForQuarantine
            ? QuarantinePathPlanner.CreatePreview(reportItem.Path, quarantineRoot)
            : null;

        return new CleanupPlanItem(
            Path: reportItem.Path,
            Action: action,
            RiskLevel: reportItem.Risk.Level,
            Reasons: reasons,
            QuarantinePreview: quarantinePreview);
    }

    private static CleanupPlanAction ChooseAction(ScanReportItem reportItem, List<string> reasons)
    {
        if (reportItem.Risk.Level == RiskLevel.Blocked)
        {
            reasons.Add("Blocked risk level must be kept.");
            reasons.AddRange(reportItem.Risk.Blockers);
            return CleanupPlanAction.Keep;
        }

        if (reportItem.Evidence.Any(evidence => evidence.Type == EvidenceType.CollectionFailure))
        {
            reasons.Add("Evidence collection failed");
            return CleanupPlanAction.Keep;
        }

        if (reportItem.Evidence.Any(evidence => ActiveReferenceEvidenceTypes.Contains(evidence.Type)))
        {
            reasons.Add("Active reference or installed-application evidence requires keeping the item.");
            return CleanupPlanAction.Keep;
        }

        if (reportItem.Risk.Level == RiskLevel.HighRisk)
        {
            reasons.Add("High risk items are not eligible for cleanup planning.");
            return CleanupPlanAction.Keep;
        }

        if (HasKnownCleanupRule(reportItem)
            && (reportItem.Risk.Level == RiskLevel.LowRisk || reportItem.Risk.Level == RiskLevel.SafeCandidate))
        {
            reasons.Add("Known cleanup rule matched; manual review required before quarantine.");
            return CleanupPlanAction.ReviewForQuarantine;
        }

        if (HasKnownCleanupRule(reportItem))
        {
            reasons.Add("Known cleanup rule matched, but current risk evidence is not enough for quarantine review.");
            return CleanupPlanAction.ReportOnly;
        }

        reasons.Add("Insufficient evidence for a cleanup action; report only.");
        return CleanupPlanAction.ReportOnly;
    }

    private static bool HasKnownCleanupRule(ScanReportItem reportItem)
    {
        return reportItem.Evidence.Any(evidence => evidence.Type == EvidenceType.KnownCleanupRule);
    }
}
