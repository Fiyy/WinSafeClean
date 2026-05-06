using System.Security.Cryptography;
using System.Text;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Planning;

public static class QuarantinePathPlanner
{
    private static readonly HashSet<char> InvalidFileNameCharacters = new(
        Path.GetInvalidFileNameChars().Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*']));

    public static QuarantinePreview CreatePreview(string sourcePath, string quarantineRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var normalizedQuarantineRoot = NormalizeQuarantineRoot(quarantineRoot);
        var restorePlanId = CreateRestorePlanId(sourcePath);
        var safeLeafName = SanitizeLeafName(GetLeafName(sourcePath));

        return new QuarantinePreview(
            OriginalPath: sourcePath,
            ProposedQuarantinePath: Path.Combine(normalizedQuarantineRoot, "items", $"{restorePlanId}-{safeLeafName}"),
            RestoreMetadataPath: Path.Combine(normalizedQuarantineRoot, "restore", $"{restorePlanId}.restore.json"),
            RestorePlanId: restorePlanId,
            RequiresManualConfirmation: true,
            Warnings:
            [
                "Quarantine preview only; no file operation has been executed.",
                "Manual confirmation is required before any quarantine action."
            ]);
    }

    public static string GetDefaultQuarantineRoot()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var root = string.IsNullOrWhiteSpace(localApplicationData)
            ? Path.GetTempPath()
            : localApplicationData;

        return Path.Combine(root, "WinSafeClean", "Quarantine");
    }

    public static string NormalizeQuarantineRoot(string quarantineRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(quarantineRoot);

        var expandedRoot = Environment.ExpandEnvironmentVariables(quarantineRoot);
        var normalizedRoot = TrimTrailingSeparators(Path.GetFullPath(expandedRoot));

        if (PathRiskClassifier.Assess(normalizedRoot).Level == RiskLevel.Blocked)
        {
            throw new ArgumentException("Quarantine root must not target a protected Windows path.", nameof(quarantineRoot));
        }

        return normalizedRoot;
    }

    private static string TrimTrailingSeparators(string path)
    {
        var root = Path.GetPathRoot(path);
        var minimumLength = string.IsNullOrEmpty(root) ? 0 : root.Length;

        var end = path.Length;
        while (end > minimumLength
            && (path[end - 1] == Path.DirectorySeparatorChar || path[end - 1] == Path.AltDirectorySeparatorChar))
        {
            end--;
        }

        return end == path.Length ? path : path[..end];
    }

    private static string CreateRestorePlanId(string sourcePath)
    {
        var normalizedSource = sourcePath.Trim().ToUpperInvariant();
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(normalizedSource));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string GetLeafName(string sourcePath)
    {
        var trimmed = sourcePath.Trim().TrimEnd('\\', '/');
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return "item";
        }

        var separatorIndex = trimmed.LastIndexOfAny(['\\', '/']);
        return separatorIndex >= 0
            ? trimmed[(separatorIndex + 1)..]
            : trimmed;
    }

    private static string SanitizeLeafName(string leafName)
    {
        var builder = new StringBuilder(leafName.Length);

        foreach (var character in leafName)
        {
            builder.Append(char.IsControl(character) || InvalidFileNameCharacters.Contains(character)
                ? '_'
                : character);
        }

        var sanitized = builder.ToString().Trim().TrimEnd('.');
        if (sanitized.Length > 80)
        {
            sanitized = sanitized[..80].Trim().TrimEnd('.');
        }

        return string.IsNullOrWhiteSpace(sanitized)
            ? "item"
            : sanitized;
    }
}
