using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class FileAssociationEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsFileAssociationSource fileAssociationSource;

    public FileAssociationEvidenceProvider()
        : this(CreateDefaultFileAssociationSource())
    {
    }

    public FileAssociationEvidenceProvider(IWindowsFileAssociationSource fileAssociationSource)
    {
        ArgumentNullException.ThrowIfNull(fileAssociationSource);

        this.fileAssociationSource = fileAssociationSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var association in fileAssociationSource.GetFileAssociations())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var commandExecutablePath = ServiceImagePathParser.TryGetExecutablePath(association.Command);
            if (commandExecutablePath is null
                || !commandExecutablePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.FileAssociationReference,
                Source: FormatSource(association),
                Confidence: 0.85,
                Message: $"Windows file association {association.Verb} command references this file: {association.Command}"));
        }

        return evidence;
    }

    private static string FormatSource(WindowsFileAssociationRecord association)
    {
        var owner = string.IsNullOrWhiteSpace(association.ProgId)
            ? association.Extension
            : $"{association.Extension} -> {association.ProgId}";

        return $@"{association.Scope}\{association.RegistryPath}: {owner} {association.Verb}";
    }

    private static IWindowsFileAssociationSource CreateDefaultFileAssociationSource()
    {
        return OperatingSystem.IsWindows()
            ? new RegistryWindowsFileAssociationSource()
            : EmptyWindowsFileAssociationSource.Instance;
    }

    private sealed class EmptyWindowsFileAssociationSource : IWindowsFileAssociationSource
    {
        public static readonly EmptyWindowsFileAssociationSource Instance = new();

        private EmptyWindowsFileAssociationSource()
        {
        }

        public IReadOnlyList<WindowsFileAssociationRecord> GetFileAssociations()
        {
            return [];
        }
    }
}
