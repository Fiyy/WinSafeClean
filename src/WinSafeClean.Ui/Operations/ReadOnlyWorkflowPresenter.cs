namespace WinSafeClean.Ui.Operations;

public static class ReadOnlyWorkflowPresenter
{
    private const string SafetyBoundary = "No delete or clean command is available from this UI.";

    public static ReadOnlyWorkflowView Create(ReadOnlyWorkflowSnapshot snapshot)
    {
        string scanStatus = snapshot.ScanCompleted
            ? "Done"
            : snapshot.HasScanTarget ? "Ready" : "Needs target";

        string planStatus = snapshot.PlanCompleted
            ? "Done"
            : snapshot.ScanCompleted ? "Ready" : "Pending";

        string preflightStatus = snapshot.PreflightCompleted
            ? "Done"
            : snapshot.PreflightInputsReady ? "Ready" : snapshot.PlanCompleted ? "Needs candidate" : "Pending";

        if (snapshot.PreflightCompleted)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Safety checklist is loaded.",
                PrimaryActionText: "Review Safety Check",
                PrimaryAction: ReadOnlyWorkflowAction.ReviewPreflight,
                PrimaryActionEnabled: true,
                CurrentStepTitle: "Review safety check",
                CurrentStepDetail: "Only build a guarded CLI handoff if the checklist result is acceptable.",
                SafetyBoundary: SafetyBoundary);
        }

        if (snapshot.PreflightInputsReady)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Run Safety Check to validate the selected candidate.",
                PrimaryActionText: "Run Safety Check",
                PrimaryAction: ReadOnlyWorkflowAction.RunPreflight,
                PrimaryActionEnabled: true,
                CurrentStepTitle: "Run safety check",
                CurrentStepDetail: "Validate the selected candidate and restore metadata before building any file-moving command.",
                SafetyBoundary: SafetyBoundary);
        }

        if (snapshot.PlanCompleted)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Select a quarantine candidate in Cleanup Plan, then prepare Safety Check.",
                PrimaryActionText: "Review Plan",
                PrimaryAction: ReadOnlyWorkflowAction.ReviewPlan,
                PrimaryActionEnabled: true,
                CurrentStepTitle: "Review plan result",
                CurrentStepDetail: "Select a ReviewForQuarantine candidate only if its evidence and risk level are acceptable.",
                SafetyBoundary: SafetyBoundary);
        }

        if (snapshot.ScanCompleted)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Create Plan to review cleanup candidates.",
                PrimaryActionText: "Create Plan",
                PrimaryAction: ReadOnlyWorkflowAction.RunPlan,
                PrimaryActionEnabled: true,
                CurrentStepTitle: "Create cleanup plan",
                CurrentStepDetail: "Use the same target to classify each item as keep, report-only or review for quarantine.",
                SafetyBoundary: SafetyBoundary);
        }

        return new ReadOnlyWorkflowView(
            scanStatus,
            planStatus,
            preflightStatus,
            StatusText: snapshot.HasScanTarget ? "Run Evidence Scan to collect evidence." : "Choose a target path to start review.",
            PrimaryActionText: "Run Evidence Scan",
            PrimaryAction: ReadOnlyWorkflowAction.RunScan,
            PrimaryActionEnabled: snapshot.HasScanTarget,
            CurrentStepTitle: snapshot.HasScanTarget ? "Run evidence scan" : "Choose review target",
            CurrentStepDetail: snapshot.HasScanTarget
                ? "Create a JSON scan report so the review has size, risk and evidence data."
                : "Pick a Quick Start location or choose a folder/file before running the evidence scan.",
            SafetyBoundary: SafetyBoundary);
    }
}

public sealed record ReadOnlyWorkflowSnapshot(
    bool HasScanTarget,
    bool ScanCompleted,
    bool PlanCompleted,
    bool PreflightInputsReady,
    bool PreflightCompleted);

public sealed record ReadOnlyWorkflowView(
    string ScanStatus,
    string PlanStatus,
    string PreflightStatus,
    string StatusText,
    string PrimaryActionText,
    ReadOnlyWorkflowAction PrimaryAction,
    bool PrimaryActionEnabled,
    string CurrentStepTitle,
    string CurrentStepDetail,
    string SafetyBoundary);

public enum ReadOnlyWorkflowAction
{
    RunScan,
    RunPlan,
    ReviewPlan,
    RunPreflight,
    ReviewPreflight
}
