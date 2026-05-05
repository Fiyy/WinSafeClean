using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class UninstallRegistryEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsUninstallEntrySource uninstallEntrySource;

    public UninstallRegistryEvidenceProvider()
        : this(CreateDefaultUninstallEntrySource())
    {
    }

    public UninstallRegistryEvidenceProvider(IWindowsUninstallEntrySource uninstallEntrySource)
    {
        ArgumentNullException.ThrowIfNull(uninstallEntrySource);

        this.uninstallEntrySource = uninstallEntrySource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var uninstallEntry in uninstallEntrySource.GetUninstallEntries())
        {
            cancellationToken.ThrowIfCancellationRequested();

            AddCommandEvidence(evidence, normalizedPath, uninstallEntry, "UninstallString", uninstallEntry.UninstallString);
            AddCommandEvidence(evidence, normalizedPath, uninstallEntry, "QuietUninstallString", uninstallEntry.QuietUninstallString);
            AddCommandEvidence(evidence, normalizedPath, uninstallEntry, "DisplayIcon", StripDisplayIconIndex(uninstallEntry.DisplayIcon));

            if (IsPathInsideInstallLocation(normalizedPath, uninstallEntry.InstallLocation))
            {
                evidence.Add(new EvidenceRecord(
                    Type: EvidenceType.InstalledApplication,
                    Source: FormatSource(uninstallEntry),
                    Confidence: 0.8,
                    Message: $"Path is under installed application InstallLocation: {uninstallEntry.InstallLocation}"));
            }
        }

        return evidence;
    }

    private static void AddCommandEvidence(
        List<EvidenceRecord> evidence,
        string normalizedPath,
        WindowsUninstallEntryRecord uninstallEntry,
        string fieldName,
        string? command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        var commandExecutablePath = ServiceImagePathParser.TryGetExecutablePath(command);
        if (commandExecutablePath is null
            || !commandExecutablePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        evidence.Add(new EvidenceRecord(
            Type: EvidenceType.UninstallRegistryReference,
            Source: FormatSource(uninstallEntry),
            Confidence: 0.9,
            Message: $"{fieldName} references this file: {command}"));
    }

    private static bool IsPathInsideInstallLocation(string normalizedPath, string? installLocation)
    {
        if (string.IsNullOrWhiteSpace(installLocation))
        {
            return false;
        }

        try
        {
            var expandedInstallLocation = Environment.ExpandEnvironmentVariables(installLocation.Trim().Trim('"'));
            var normalizedInstallLocation = Path.GetFullPath(expandedInstallLocation)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalizedPath.Equals(normalizedInstallLocation, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(
                    normalizedInstallLocation + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(
                    normalizedInstallLocation + Path.AltDirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    private static string? StripDisplayIconIndex(string? displayIcon)
    {
        if (string.IsNullOrWhiteSpace(displayIcon))
        {
            return null;
        }

        var commaIndex = displayIcon.LastIndexOf(',');
        if (commaIndex <= 0)
        {
            return displayIcon;
        }

        return int.TryParse(displayIcon[(commaIndex + 1)..].Trim(), out _)
            ? displayIcon[..commaIndex]
            : displayIcon;
    }

    private static string FormatSource(WindowsUninstallEntryRecord uninstallEntry)
    {
        var displayName = string.IsNullOrWhiteSpace(uninstallEntry.DisplayName)
            ? uninstallEntry.KeyName
            : uninstallEntry.DisplayName;

        return $@"{uninstallEntry.Scope}\{uninstallEntry.Location}\{uninstallEntry.KeyName}: {displayName}";
    }

    private static IWindowsUninstallEntrySource CreateDefaultUninstallEntrySource()
    {
        return OperatingSystem.IsWindows()
            ? new RegistryWindowsUninstallEntrySource()
            : EmptyWindowsUninstallEntrySource.Instance;
    }

    private sealed class EmptyWindowsUninstallEntrySource : IWindowsUninstallEntrySource
    {
        public static readonly EmptyWindowsUninstallEntrySource Instance = new();

        private EmptyWindowsUninstallEntrySource()
        {
        }

        public IReadOnlyList<WindowsUninstallEntryRecord> GetUninstallEntries()
        {
            return [];
        }
    }
}
