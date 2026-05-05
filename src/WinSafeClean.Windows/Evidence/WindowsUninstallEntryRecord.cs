namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsUninstallEntryRecord(
    string Scope,
    string Location,
    string KeyName,
    string? DisplayName,
    string? InstallLocation,
    string? UninstallString,
    string? QuietUninstallString,
    string? DisplayIcon);
