using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class PathEnvironmentEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsPathEnvironmentSource pathEnvironmentSource;

    public PathEnvironmentEvidenceProvider()
        : this(CreateDefaultPathEnvironmentSource())
    {
    }

    public PathEnvironmentEvidenceProvider(IWindowsPathEnvironmentSource pathEnvironmentSource)
    {
        ArgumentNullException.ThrowIfNull(pathEnvironmentSource);

        this.pathEnvironmentSource = pathEnvironmentSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = NormalizeCandidatePath(path);
        var normalizedParent = GetNormalizedParentPath(normalizedPath);
        var evidence = new List<EvidenceRecord>();

        foreach (var record in pathEnvironmentSource.GetPathEnvironmentRecords())
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var entry in SplitPathEntries(record.Value))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var normalizedEntry = TryNormalizePathEntry(entry);
                if (normalizedEntry is null)
                {
                    continue;
                }

                if (normalizedPath.Equals(normalizedEntry, StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add(new EvidenceRecord(
                        Type: EvidenceType.PathEnvironmentReference,
                        Source: $"{record.Scope} PATH",
                        Confidence: 0.75,
                        Message: $"PATH entry references this directory: {entry}"));
                    continue;
                }

                if (normalizedParent is not null
                    && normalizedParent.Equals(normalizedEntry, StringComparison.OrdinalIgnoreCase))
                {
                    evidence.Add(new EvidenceRecord(
                        Type: EvidenceType.PathEnvironmentReference,
                        Source: $"{record.Scope} PATH",
                        Confidence: 0.7,
                        Message: $"This file's parent directory is listed in PATH: {entry}"));
                }
            }
        }

        return evidence;
    }

    private static string NormalizeCandidatePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string? GetNormalizedParentPath(string normalizedPath)
    {
        var parent = Path.GetDirectoryName(normalizedPath);
        return string.IsNullOrWhiteSpace(parent)
            ? null
            : Path.GetFullPath(parent).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static IEnumerable<string> SplitPathEntries(string pathValue)
    {
        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(entry => !string.IsNullOrWhiteSpace(entry));
    }

    private static string? TryNormalizePathEntry(string pathEntry)
    {
        try
        {
            var expandedEntry = Environment.ExpandEnvironmentVariables(pathEntry.Trim().Trim('"'));
            if (string.IsNullOrWhiteSpace(expandedEntry))
            {
                return null;
            }

            return Path.GetFullPath(expandedEntry)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
        }
    }

    private static IWindowsPathEnvironmentSource CreateDefaultPathEnvironmentSource()
    {
        return OperatingSystem.IsWindows()
            ? new SystemWindowsPathEnvironmentSource()
            : EmptyWindowsPathEnvironmentSource.Instance;
    }

    private sealed class EmptyWindowsPathEnvironmentSource : IWindowsPathEnvironmentSource
    {
        public static readonly EmptyWindowsPathEnvironmentSource Instance = new();

        private EmptyWindowsPathEnvironmentSource()
        {
        }

        public IReadOnlyList<WindowsPathEnvironmentRecord> GetPathEnvironmentRecords()
        {
            return [];
        }
    }
}
