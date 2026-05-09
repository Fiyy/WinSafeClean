namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsShortcutRecord(
    string ShortcutPath,
    string TargetPath,
    string? Arguments,
    string? WorkingDirectory);
