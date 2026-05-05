namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsServiceRecord(
    string Name,
    string? DisplayName,
    string? ImagePath);
