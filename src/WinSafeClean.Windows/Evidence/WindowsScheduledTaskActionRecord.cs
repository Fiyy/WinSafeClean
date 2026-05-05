namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsScheduledTaskActionRecord(
    string Command,
    string? Arguments,
    string? WorkingDirectory);
