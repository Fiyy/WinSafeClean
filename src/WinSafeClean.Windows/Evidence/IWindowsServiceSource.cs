namespace WinSafeClean.Windows.Evidence;

public interface IWindowsServiceSource
{
    IReadOnlyList<WindowsServiceRecord> GetServices();
}
