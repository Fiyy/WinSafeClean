namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsScheduledTaskRecord(
    string Path,
    string? Uri,
    IReadOnlyList<WindowsScheduledTaskActionRecord> Actions);
