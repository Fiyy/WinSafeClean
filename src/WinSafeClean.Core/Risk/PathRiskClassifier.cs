namespace WinSafeClean.Core.Risk;

public static class PathRiskClassifier
{
    private static readonly ForbiddenPathRule[] ForbiddenPathRules =
    [
        new(
            @"Windows\Installer",
            SuggestedAction.Keep,
            "Windows Installer cache must not be cleaned manually."),
        new(
            @"Windows\WinSxS",
            SuggestedAction.SuggestWindowsTool,
            "WinSxS is managed by Windows servicing and should only be cleaned through supported Windows tools."),
        new(
            @"Windows\System32\DriverStore",
            SuggestedAction.SuggestWindowsTool,
            "DriverStore contains driver packages and must not be cleaned manually."),
        new(
            @"Windows\System32",
            SuggestedAction.SuggestWindowsTool,
            "System32 contains operating system components and must not be cleaned manually."),
        new(
            @"Windows\SysWOW64",
            SuggestedAction.SuggestWindowsTool,
            "SysWOW64 contains operating system components and must not be cleaned manually."),
        new(
            @"Windows\SystemApps",
            SuggestedAction.SuggestWindowsTool,
            "SystemApps contains Windows app components and must not be cleaned manually."),
        new(
            @"Windows\servicing",
            SuggestedAction.SuggestWindowsTool,
            "Windows servicing data must only be changed through supported Windows tools."),
        new(
            @"Windows\INF",
            SuggestedAction.SuggestWindowsTool,
            "Windows INF data is used by the operating system and drivers."),
    ];

    public static RiskAssessment Assess(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = NormalizePath(path);

        foreach (var rule in ForbiddenPathRules)
        {
            if (TryBuildProtectedPath(normalizedPath, rule.RelativePath, out var protectedPath)
                && IsSamePathOrChild(normalizedPath, protectedPath))
            {
                return RiskAssessment.Blocked(rule.SuggestedAction, rule.Blocker);
            }
        }

        return RiskAssessment.Unknown("No path-level rule matched this item.");
    }

    private static bool IsSamePathOrChild(string path, string parentPath)
    {
        return path.Equals(parentPath, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(parentPath + @"\", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryBuildProtectedPath(string normalizedPath, string relativePath, out string protectedPath)
    {
        var root = Path.GetPathRoot(normalizedPath);

        if (string.IsNullOrWhiteSpace(root))
        {
            protectedPath = string.Empty;
            return false;
        }

        protectedPath = TrimTrailingSeparators(Path.Combine(root, relativePath));
        return true;
    }

    private static string NormalizePath(string path)
    {
        var normalized = path.Trim().Replace('/', '\\');
        normalized = RemoveWindowsDevicePrefix(normalized);
        normalized = ConvertLocalAdminShareToDrivePath(normalized);

        try
        {
            normalized = Path.GetFullPath(normalized);
        }
        catch (ArgumentException)
        {
            normalized = CollapseRepeatedSeparators(normalized);
        }
        catch (NotSupportedException)
        {
            normalized = CollapseRepeatedSeparators(normalized);
        }

        return TrimTrailingSeparators(normalized);
    }

    private static string RemoveWindowsDevicePrefix(string path)
    {
        const string extendedUncPrefix = @"\\?\UNC\";
        const string extendedPathPrefix = @"\\?\";
        const string devicePathPrefix = @"\\.\";

        if (path.StartsWith(extendedUncPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return @"\\" + path[extendedUncPrefix.Length..];
        }

        if (path.StartsWith(extendedPathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[extendedPathPrefix.Length..];
        }

        if (path.StartsWith(devicePathPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return path[devicePathPrefix.Length..];
        }

        return path;
    }

    private static string ConvertLocalAdminShareToDrivePath(string path)
    {
        if (!path.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return path;
        }

        var parts = path.Split('\\', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 3 || !IsLocalHost(parts[0]) || !IsDriveAdminShare(parts[1]))
        {
            return path;
        }

        var drive = char.ToUpperInvariant(parts[1][0]);
        return $@"{drive}:\{string.Join('\\', parts[2..])}";
    }

    private static bool IsLocalHost(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase)
            || host.Equals(".", StringComparison.OrdinalIgnoreCase)
            || host.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsDriveAdminShare(string share)
    {
        return share.Length == 2
            && share[1] == '$'
            && char.IsAsciiLetter(share[0]);
    }

    private static string CollapseRepeatedSeparators(string path)
    {
        var root = Path.GetPathRoot(path);

        if (string.IsNullOrEmpty(root))
        {
            return string.Join('\\', path.Split('\\', StringSplitOptions.RemoveEmptyEntries));
        }

        var remainder = path[root.Length..];
        return root + string.Join('\\', remainder.Split('\\', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string TrimTrailingSeparators(string path)
    {
        var root = Path.GetPathRoot(path);

        if (!string.IsNullOrEmpty(root) && path.Length <= root.Length)
        {
            return path;
        }

        return path.TrimEnd('\\');
    }

    private sealed record ForbiddenPathRule(string RelativePath, SuggestedAction SuggestedAction, string Blocker);
}
