namespace WinSafeClean.Ui.Operations;

public static class ReadOnlyWorkflowPresenter
{
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
                StatusText: "Preflight checklist is loaded.",
                PrimaryActionText: "Review Preflight",
                PrimaryAction: ReadOnlyWorkflowAction.ReviewPreflight,
                PrimaryActionEnabled: true);
        }

        if (snapshot.PreflightInputsReady)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Run Preflight to validate the selected candidate.",
                PrimaryActionText: "Run Preflight",
                PrimaryAction: ReadOnlyWorkflowAction.RunPreflight,
                PrimaryActionEnabled: true);
        }

        if (snapshot.PlanCompleted)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Select a quarantine candidate in Cleanup Plan, then prepare Preflight.",
                PrimaryActionText: "Review Plan",
                PrimaryAction: ReadOnlyWorkflowAction.ReviewPlan,
                PrimaryActionEnabled: true);
        }

        if (snapshot.ScanCompleted)
        {
            return new ReadOnlyWorkflowView(
                scanStatus,
                planStatus,
                preflightStatus,
                StatusText: "Run Plan to review cleanup candidates.",
                PrimaryActionText: "Run Plan",
                PrimaryAction: ReadOnlyWorkflowAction.RunPlan,
                PrimaryActionEnabled: true);
        }

        return new ReadOnlyWorkflowView(
            scanStatus,
            planStatus,
            preflightStatus,
            StatusText: snapshot.HasScanTarget ? "Run Scan to collect evidence." : "Choose a target path to start scan.",
            PrimaryActionText: "Run Scan",
            PrimaryAction: ReadOnlyWorkflowAction.RunScan,
            PrimaryActionEnabled: snapshot.HasScanTarget);
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
    bool PrimaryActionEnabled);

public enum ReadOnlyWorkflowAction
{
    RunScan,
    RunPlan,
    ReviewPlan,
    RunPreflight,
    ReviewPreflight
}
