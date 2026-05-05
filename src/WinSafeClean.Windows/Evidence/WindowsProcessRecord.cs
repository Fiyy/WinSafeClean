namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsProcessRecord(
    int ProcessId,
    string ProcessName,
    string? MainModuleFilePath);
