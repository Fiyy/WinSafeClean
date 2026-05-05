using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class StartupEntryEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsStartupEntrySource startupEntrySource;

    public StartupEntryEvidenceProvider()
        : this(CreateDefaultStartupEntrySource())
    {
    }

    public StartupEntryEvidenceProvider(IWindowsStartupEntrySource startupEntrySource)
    {
        ArgumentNullException.ThrowIfNull(startupEntrySource);

        this.startupEntrySource = startupEntrySource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var startupEntry in startupEntrySource.GetStartupEntries())
        {
            var startupExecutablePath = ServiceImagePathParser.TryGetExecutablePath(startupEntry.Command);
            if (startupExecutablePath is null
                || !startupExecutablePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.StartupReference,
                Source: FormatSource(startupEntry),
                Confidence: 0.85,
                Message: $"Startup entry references this file: {startupEntry.Command}"));
        }

        return evidence;
    }

    private static string FormatSource(WindowsStartupEntryRecord startupEntry)
    {
        return $@"{startupEntry.Scope}\{startupEntry.Location}: {startupEntry.Name}";
    }

    private static IWindowsStartupEntrySource CreateDefaultStartupEntrySource()
    {
        return OperatingSystem.IsWindows()
            ? new RegistryWindowsStartupEntrySource()
            : EmptyWindowsStartupEntrySource.Instance;
    }

    private sealed class EmptyWindowsStartupEntrySource : IWindowsStartupEntrySource
    {
        public static readonly EmptyWindowsStartupEntrySource Instance = new();

        private EmptyWindowsStartupEntrySource()
        {
        }

        public IReadOnlyList<WindowsStartupEntryRecord> GetStartupEntries()
        {
            return [];
        }
    }
}
