using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Planning;

public static class CleanupPlanGenerator
{
    private const string CurrentSchemaVersion = "0.1";

    private static readonly HashSet<EvidenceType> ActiveReferenceEvidenceTypes =
    [
        EvidenceType.ServiceReference,
        EvidenceType.ScheduledTaskReference,
        EvidenceType.StartupReference,
        EvidenceType.UninstallRegistryReference,
        EvidenceType.RunningProcessReference,
        EvidenceType.InstalledApplication
    ];

    public static CleanupPlan Generate(ScanReport report, DateTimeOffset createdAt)
    {
        ArgumentNullException.ThrowIfNull(report);

        return new CleanupPlan(
            SchemaVersion: CurrentSchemaVersion,
            CreatedAt: createdAt,
            Items: report.Items.Select(CreatePlanItem).ToArray());
    }

    private static CleanupPlanItem CreatePlanItem(ScanReportItem reportItem)
    {
        var reasons = new List<string>();
        var action = ChooseAction(reportItem, reasons);

        return new CleanupPlanItem(
            Path: reportItem.Path,
            Action: action,
            RiskLevel: reportItem.Risk.Level,
            Reasons: reasons);
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

        reasons.Add("Insufficient evidence for a cleanup action; report only.");
        return CleanupPlanAction.ReportOnly;
    }

    private static bool HasKnownCleanupRule(ScanReportItem reportItem)
    {
        return reportItem.Evidence.Any(evidence => evidence.Type == EvidenceType.KnownCleanupRule);
    }
}
