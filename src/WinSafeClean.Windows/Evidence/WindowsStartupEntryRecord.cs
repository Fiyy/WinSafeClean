namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsStartupEntryRecord(
    string Scope,
    string Location,
    string Name,
    string Command);
