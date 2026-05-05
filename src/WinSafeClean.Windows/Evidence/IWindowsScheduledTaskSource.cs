namespace WinSafeClean.Windows.Evidence;

public interface IWindowsScheduledTaskSource
{
    IReadOnlyList<WindowsScheduledTaskRecord> GetScheduledTasks();
}
