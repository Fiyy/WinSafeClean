namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantineExecutionResult(
    bool Succeeded,
    QuarantinePreflightChecklist PreflightChecklist,
    QuarantineOperationLog OperationLog,
    string? ErrorMessage);
