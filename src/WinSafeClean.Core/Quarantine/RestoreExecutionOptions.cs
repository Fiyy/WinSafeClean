namespace WinSafeClean.Core.Quarantine;

public sealed record RestoreExecutionOptions(
    bool ManualConfirmationProvided,
    string OperationId,
    string RunId,
    string? OperationLogPath = null,
    bool AllowLegacyMetadataWithoutContentHash = false);
