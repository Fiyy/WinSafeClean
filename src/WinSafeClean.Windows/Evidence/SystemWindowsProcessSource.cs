using System.ComponentModel;
using System.Diagnostics;
using System.Security;

namespace WinSafeClean.Windows.Evidence;

public sealed class SystemWindowsProcessSource : IWindowsProcessSource
{
    public IReadOnlyList<WindowsProcessRecord> GetProcesses()
    {
        var records = new List<WindowsProcessRecord>();

        foreach (var process in Process.GetProcesses())
        {
            using (process)
            {
                var record = TryReadProcess(process);
                if (record is not null)
                {
                    records.Add(record);
                }
            }
        }

        return records;
    }

    private static WindowsProcessRecord? TryReadProcess(Process process)
    {
        try
        {
            return new WindowsProcessRecord(
                ProcessId: process.Id,
                ProcessName: process.ProcessName,
                MainModuleFilePath: TryReadMainModuleFilePath(process));
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
    }

    private static string? TryReadMainModuleFilePath(Process process)
    {
        try
        {
            return process.MainModule?.FileName;
        }
        catch (Win32Exception)
        {
            return null;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (SecurityException)
        {
            return null;
        }
    }
}
