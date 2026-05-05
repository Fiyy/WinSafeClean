namespace WinSafeClean.Windows.Evidence;

public interface IWindowsProcessSource
{
    IReadOnlyList<WindowsProcessRecord> GetProcesses();
}
