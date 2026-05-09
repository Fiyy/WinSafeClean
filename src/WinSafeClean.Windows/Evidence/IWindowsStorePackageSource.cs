namespace WinSafeClean.Windows.Evidence;

public interface IWindowsStorePackageSource
{
    IReadOnlyList<WindowsStorePackageRecord> GetStorePackages();
}
