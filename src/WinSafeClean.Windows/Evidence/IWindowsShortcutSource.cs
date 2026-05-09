namespace WinSafeClean.Windows.Evidence;

public interface IWindowsShortcutSource
{
    IReadOnlyList<WindowsShortcutRecord> GetShortcuts();
}
