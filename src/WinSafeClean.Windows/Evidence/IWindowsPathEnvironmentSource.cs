namespace WinSafeClean.Windows.Evidence;

public interface IWindowsPathEnvironmentSource
{
    IReadOnlyList<WindowsPathEnvironmentRecord> GetPathEnvironmentRecords();
}
