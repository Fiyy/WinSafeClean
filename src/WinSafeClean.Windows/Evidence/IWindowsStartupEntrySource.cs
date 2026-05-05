namespace WinSafeClean.Windows.Evidence;

public interface IWindowsStartupEntrySource
{
    IReadOnlyList<WindowsStartupEntryRecord> GetStartupEntries();
}
