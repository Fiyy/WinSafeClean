namespace WinSafeClean.Ui.ViewModels;

public sealed record ResultDispositionAdvice(
    string Title,
    string Message,
    string NextStep,
    bool CanPreparePreflight);

public static class ResultDispositionAdvisor
{
    public static ResultDispositionAdvice ForScanItem(ScanReportOverviewItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.RiskLevel.Equals("Blocked", StringComparison.OrdinalIgnoreCase)
            || item.SuggestedAction.Equals("Keep", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultDispositionAdvice(
                Title: "Keep this item",
                Message: "This item is blocked or marked keep. It may belong to Windows, an installed app, or an active dependency.",
                NextStep: "Do not manually clean it. Keep it in place and use Windows-supported cleanup tools when applicable.",
                CanPreparePreflight: false);
        }

        if (item.SuggestedAction.Equals("ReportOnly", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultDispositionAdvice(
                Title: "Review in Plan",
                Message: "This scan report is evidence, not a cleanup instruction. A cleanup plan is required before any file-moving handoff.",
                NextStep: "Run Plan for this target, then review whether the item remains report-only or becomes a quarantine candidate.",
                CanPreparePreflight: false);
        }

        return new ResultDispositionAdvice(
            Title: "Review before action",
            Message: "The scan result needs a cleanup plan and evidence review before any file-moving step.",
            NextStep: "Run Plan, review evidence and blockers, and only continue if the plan creates a quarantine candidate.",
            CanPreparePreflight: false);
    }

    public static ResultDispositionAdvice ForPlanItem(PlanOverviewItemViewModel item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (item.Action.Equals("Keep", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultDispositionAdvice(
                Title: "Keep this item",
                Message: "The plan says this item should stay in place because risk, blockers, or protected-path rules outweigh cleanup value.",
                NextStep: "Do not build a quarantine command for this item.",
                CanPreparePreflight: false);
        }

        if (item.Action.Equals("ReportOnly", StringComparison.OrdinalIgnoreCase))
        {
            return new ResultDispositionAdvice(
                Title: "Report only",
                Message: "The plan found information worth showing, but no file-moving action is recommended for this item.",
                NextStep: "Use this result to review evidence, ownership, and size. Leave the file in place unless a future plan marks it as a quarantine candidate.",
                CanPreparePreflight: false);
        }

        if (item.Action.Equals("ReviewForQuarantine", StringComparison.OrdinalIgnoreCase))
        {
            bool hasPreview = !string.IsNullOrWhiteSpace(item.QuarantinePath)
                && !string.IsNullOrWhiteSpace(item.RestoreMetadataPath);

            if (!hasPreview)
            {
                return new ResultDispositionAdvice(
                    Title: "Missing quarantine preview",
                    Message: "This item is marked for quarantine review, but the plan cannot prepare preflight without preview paths.",
                    NextStep: "Re-run Plan or leave the item in place until a valid quarantine preview is available.",
                    CanPreparePreflight: false);
            }

            return new ResultDispositionAdvice(
                Title: "Prepare Preflight",
                Message: "This item is a quarantine candidate. The UI can prepare preflight inputs, but it will not move the file.",
                NextStep: "Prepare Preflight, Run Preflight, then build a guarded CLI handoff only if every check is acceptable.",
                CanPreparePreflight: true);
        }

        return new ResultDispositionAdvice(
            Title: "Review before action",
            Message: "The plan action is not recognized by this UI version.",
            NextStep: "Keep the item in place and inspect the plan JSON before continuing.",
            CanPreparePreflight: false);
    }
}
