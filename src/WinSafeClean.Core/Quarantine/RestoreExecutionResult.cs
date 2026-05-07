namespace WinSafeClean.Core.Quarantine;

public sealed record RestoreExecutionResult(
    bool Succeeded,
    QuarantineOperationLog OperationLog,
    string? ErrorMessage,
    string? WarningMessage = null);
