namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantineExecutionOptions(
    bool ManualConfirmationProvided,
    string OperationId,
    string RunId,
    string? OperationLogPath = null);
