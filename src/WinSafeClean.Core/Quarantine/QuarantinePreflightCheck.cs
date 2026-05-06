namespace WinSafeClean.Core.Quarantine;

public sealed record QuarantinePreflightCheck(
    string Code,
    QuarantinePreflightCheckStatus Status,
    string Message);
