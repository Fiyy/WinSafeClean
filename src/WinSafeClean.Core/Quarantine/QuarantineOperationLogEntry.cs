namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantineOperationLogEntry(
    string OperationId,
    string RunId,
    string RestorePlanId,
    QuarantineOperationType OperationType,
    QuarantineOperationStatus Status,
    DateTimeOffset Timestamp,
    string SourcePath,
    string? TargetPath,
    string? RestoreMetadataPath,
    bool IsDryRun,
    string Actor,
    string Message);
