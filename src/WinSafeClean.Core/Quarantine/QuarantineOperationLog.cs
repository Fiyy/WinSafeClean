namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantineOperationLog(
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<QuarantineOperationLogEntry> Entries);
