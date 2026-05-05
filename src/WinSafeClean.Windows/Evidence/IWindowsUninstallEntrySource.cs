namespace WinSafeClean.Windows.Evidence;

public interface IWindowsUninstallEntrySource
{
    IReadOnlyList<WindowsUninstallEntryRecord> GetUninstallEntries();
}
