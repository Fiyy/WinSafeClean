namespace WinSafeClean.Windows.Evidence;

public sealed class SystemWindowsPathEnvironmentSource : IWindowsPathEnvironmentSource
{
    public IReadOnlyList<WindowsPathEnvironmentRecord> GetPathEnvironmentRecords()
    {
        var records = new List<WindowsPathEnvironmentRecord>();

        AddRecord(records, "Process", Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process));
        AddRecord(records, "User", Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User));
        AddRecord(records, "Machine", Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Machine));

        return records;
    }

    private static void AddRecord(List<WindowsPathEnvironmentRecord> records, string scope, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            records.Add(new WindowsPathEnvironmentRecord(scope, value));
        }
    }
}
