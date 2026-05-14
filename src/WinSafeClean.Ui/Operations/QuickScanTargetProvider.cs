using System.IO;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Ui.Operations;

public sealed record QuickScanTarget(
    string Key,
    string DisplayName,
    string Path,
    string Description);

public sealed record QuickScanTargetPathSource(
    string UserProfilePath,
    string DesktopPath,
    string LocalAppDataPath,
    string TempPath);

public static class QuickScanTargetProvider
{
    public static IReadOnlyList<QuickScanTarget> CreateDefault()
    {
        return CreateDefault(new QuickScanTargetPathSource(
            UserProfilePath: Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            DesktopPath: Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),
            LocalAppDataPath: Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            TempPath: Path.GetTempPath()));
    }

    public static IReadOnlyList<QuickScanTarget> CreateDefault(QuickScanTargetPathSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var downloadsPath = string.IsNullOrWhiteSpace(source.UserProfilePath)
            ? string.Empty
            : Path.Combine(source.UserProfilePath, "Downloads");

        QuickScanTarget[] candidates =
        [
            new(
                Key: "downloads",
                DisplayName: "Downloads",
                Path: downloadsPath,
                Description: "Start a read-only review of user downloads."),
            new(
                Key: "desktop",
                DisplayName: "Desktop",
                Path: source.DesktopPath,
                Description: "Start a read-only review of files visible on the desktop."),
            new(
                Key: "temp",
                DisplayName: "User Temp",
                Path: source.TempPath,
                Description: "Start a read-only review of the current user's temporary files."),
            new(
                Key: "local-appdata",
                DisplayName: "Local AppData",
                Path: source.LocalAppDataPath,
                Description: "Start a read-only review of per-user application data.")
        ];

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var targets = new List<QuickScanTarget>();

        foreach (var candidate in candidates)
        {
            if (!TryNormalizeSafeTargetPath(candidate.Path, out var normalizedPath))
            {
                continue;
            }

            if (!seenPaths.Add(normalizedPath))
            {
                continue;
            }

            targets.Add(candidate with { Path = normalizedPath });
        }

        return targets;
    }

    private static bool TryNormalizeSafeTargetPath(string path, out string normalizedPath)
    {
        normalizedPath = string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        try
        {
            normalizedPath = TrimTrailingSeparators(Path.GetFullPath(path.Trim()));
            return PathRiskClassifier.Assess(normalizedPath).Level != RiskLevel.Blocked;
        }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException)
        {
            return false;
        }
    }

    private static string TrimTrailingSeparators(string path)
    {
        var root = Path.GetPathRoot(path);
        if (!string.IsNullOrEmpty(root) && path.Length <= root.Length)
        {
            return path;
        }

        return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
