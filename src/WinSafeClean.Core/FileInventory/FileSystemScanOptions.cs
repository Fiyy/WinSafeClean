namespace WinSafeClean.Core.FileInventory;

public sealed record FileSystemScanOptions(int MaxItems = 1000, bool Recursive = false)
{
    public static FileSystemScanOptions Default { get; } = new();
}
