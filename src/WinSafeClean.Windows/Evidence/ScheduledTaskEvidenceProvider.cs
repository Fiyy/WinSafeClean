using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class ScheduledTaskEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsScheduledTaskSource scheduledTaskSource;

    public ScheduledTaskEvidenceProvider()
        : this(CreateDefaultScheduledTaskSource())
    {
    }

    public ScheduledTaskEvidenceProvider(IWindowsScheduledTaskSource scheduledTaskSource)
    {
        ArgumentNullException.ThrowIfNull(scheduledTaskSource);

        this.scheduledTaskSource = scheduledTaskSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var task in scheduledTaskSource.GetScheduledTasks())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var action in task.Actions)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var actionExecutablePath = ServiceImagePathParser.TryGetExecutablePath(action.Command);
                if (actionExecutablePath is null
                    || !actionExecutablePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                evidence.Add(new EvidenceRecord(
                    Type: EvidenceType.ScheduledTaskReference,
                    Source: FormatSource(task),
                    Confidence: 0.9,
                    Message: $"Scheduled task action references this file: {FormatAction(action)}"));
            }
        }

        return evidence;
    }

    private static string FormatSource(WindowsScheduledTaskRecord task)
    {
        return string.IsNullOrWhiteSpace(task.Uri) ? task.Path : task.Uri;
    }

    private static string FormatAction(WindowsScheduledTaskActionRecord action)
    {
        return string.IsNullOrWhiteSpace(action.Arguments)
            ? action.Command
            : $"{action.Command} {action.Arguments}";
    }

    private static IWindowsScheduledTaskSource CreateDefaultScheduledTaskSource()
    {
        return OperatingSystem.IsWindows()
            ? new FileSystemWindowsScheduledTaskSource()
            : EmptyWindowsScheduledTaskSource.Instance;
    }

    private sealed class EmptyWindowsScheduledTaskSource : IWindowsScheduledTaskSource
    {
        public static readonly EmptyWindowsScheduledTaskSource Instance = new();

        private EmptyWindowsScheduledTaskSource()
        {
        }

        public IReadOnlyList<WindowsScheduledTaskRecord> GetScheduledTasks()
        {
            return [];
        }
    }
}
