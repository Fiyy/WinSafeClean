using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class ShortcutEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsShortcutSource shortcutSource;

    public ShortcutEvidenceProvider()
        : this(CreateDefaultShortcutSource())
    {
    }

    public ShortcutEvidenceProvider(IWindowsShortcutSource shortcutSource)
    {
        ArgumentNullException.ThrowIfNull(shortcutSource);

        this.shortcutSource = shortcutSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = NormalizeCandidatePath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var shortcut in shortcutSource.GetShortcuts())
        {
            cancellationToken.ThrowIfCancellationRequested();

            var normalizedTarget = TryNormalizeShortcutTarget(shortcut.TargetPath);
            if (normalizedTarget is null
                || !normalizedTarget.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.ShortcutReference,
                Source: shortcut.ShortcutPath,
                Confidence: 0.8,
                Message: $"Windows shortcut references this target: {FormatTarget(shortcut)}"));
        }

        return evidence;
    }

    private static string NormalizeCandidatePath(string path)
    {
        return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    private static string? TryNormalizeShortcutTarget(string targetPath)
    {
        try
        {
            var expandedTarget = Environment.ExpandEnvironmentVariables(targetPath.Trim().Trim('"'));
            if (string.IsNullOrWhiteSpace(expandedTarget))
            {
                return null;
            }

            return Path.GetFullPath(expandedTarget)
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

    private static string FormatTarget(WindowsShortcutRecord shortcut)
    {
        return string.IsNullOrWhiteSpace(shortcut.Arguments)
            ? shortcut.TargetPath
            : $"{shortcut.TargetPath} {shortcut.Arguments}";
    }

    private static IWindowsShortcutSource CreateDefaultShortcutSource()
    {
        return OperatingSystem.IsWindows()
            ? new FileSystemWindowsShortcutSource()
            : EmptyWindowsShortcutSource.Instance;
    }

    private sealed class EmptyWindowsShortcutSource : IWindowsShortcutSource
    {
        public static readonly EmptyWindowsShortcutSource Instance = new();

        private EmptyWindowsShortcutSource()
        {
        }

        public IReadOnlyList<WindowsShortcutRecord> GetShortcuts()
        {
            return [];
        }
    }
}
