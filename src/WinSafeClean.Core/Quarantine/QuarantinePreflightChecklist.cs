namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantinePreflightChecklist(
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    bool IsExecutable,
    IReadOnlyList<QuarantinePreflightCheck> Checks);
