namespace WinSafeClean.Windows.Evidence;

internal static class ServiceImagePathParser
{
    private static readonly string[] ExecutableExtensions =
    [
        ".exe",
        ".dll",
        ".sys",
        ".cmd",
        ".bat"
    ];

    public static string? TryGetExecutablePath(string imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var expanded = Environment.ExpandEnvironmentVariables(imagePath.Trim());
        var executable = ExtractExecutablePath(expanded);
        if (string.IsNullOrWhiteSpace(executable))
        {
            return null;
        }

        executable = executable
            .Replace('/', '\\')
            .Trim();

        if (executable.StartsWith(@"\??\", StringComparison.Ordinal))
        {
            executable = executable[4..];
        }

        if (executable.StartsWith(@"\SystemRoot\", StringComparison.OrdinalIgnoreCase))
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
            if (!string.IsNullOrWhiteSpace(systemRoot))
            {
                executable = Path.Combine(systemRoot, executable[@"\SystemRoot\".Length..]);
            }
        }

        return TryGetFullPath(executable);
    }

    private static string? ExtractExecutablePath(string value)
    {
        if (value.StartsWith('"'))
        {
            var closingQuoteIndex = value.IndexOf('"', startIndex: 1);
            return closingQuoteIndex > 1
                ? value[1..closingQuoteIndex]
                : null;
        }

        var executableEndIndex = FindExecutableEndIndex(value);
        if (executableEndIndex > 0)
        {
            return value[..executableEndIndex];
        }

        var firstSpace = value.IndexOf(' ', StringComparison.Ordinal);
        return firstSpace > 0 ? value[..firstSpace] : value;
    }

    private static int FindExecutableEndIndex(string value)
    {
        var bestEndIndex = -1;

        foreach (var extension in ExecutableExtensions)
        {
            var searchStart = 0;
            while (searchStart < value.Length)
            {
                var extensionIndex = value.IndexOf(extension, searchStart, StringComparison.OrdinalIgnoreCase);
                if (extensionIndex < 0)
                {
                    break;
                }

                var endIndex = extensionIndex + extension.Length;
                if (endIndex == value.Length || char.IsWhiteSpace(value[endIndex]))
                {
                    bestEndIndex = bestEndIndex < 0 ? endIndex : Math.Min(bestEndIndex, endIndex);
                }

                searchStart = endIndex;
            }
        }

        return bestEndIndex;
    }

    private static string? TryGetFullPath(string path)
    {
        try
        {
            return Path.GetFullPath(path);
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
}
