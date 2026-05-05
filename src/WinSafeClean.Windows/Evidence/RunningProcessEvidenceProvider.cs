using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class RunningProcessEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsProcessSource processSource;

    public RunningProcessEvidenceProvider()
        : this(CreateDefaultProcessSource())
    {
    }

    public RunningProcessEvidenceProvider(IWindowsProcessSource processSource)
    {
        ArgumentNullException.ThrowIfNull(processSource);

        this.processSource = processSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var process in processSource.GetProcesses())
        {
            if (string.IsNullOrWhiteSpace(process.MainModuleFilePath))
            {
                continue;
            }

            var processPath = Path.GetFullPath(process.MainModuleFilePath);
            if (!processPath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.RunningProcessReference,
                Source: FormatSource(process),
                Confidence: 1.0,
                Message: $"Running process image path matches this file: {process.MainModuleFilePath}"));
        }

        return evidence;
    }

    private static string FormatSource(WindowsProcessRecord process)
    {
        return $"{process.ProcessName} (PID {process.ProcessId})";
    }

    private static IWindowsProcessSource CreateDefaultProcessSource()
    {
        return OperatingSystem.IsWindows()
            ? new SystemWindowsProcessSource()
            : EmptyWindowsProcessSource.Instance;
    }

    private sealed class EmptyWindowsProcessSource : IWindowsProcessSource
    {
        public static readonly EmptyWindowsProcessSource Instance = new();

        private EmptyWindowsProcessSource()
        {
        }

        public IReadOnlyList<WindowsProcessRecord> GetProcesses()
        {
            return [];
        }
    }
}
