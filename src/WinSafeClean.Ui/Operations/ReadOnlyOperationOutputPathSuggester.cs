using System.Globalization;
using System.IO;
using System.Text;

namespace WinSafeClean.Ui.Operations;

public static class ReadOnlyOperationOutputPathSuggester
{
    public static string SuggestJsonPath(string directory, string operationName, DateTimeOffset timestamp)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        string safeOperationName = SanitizeSegment(operationName);
        string timestampText = timestamp.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        string baseName = $"winsafeclean-{safeOperationName}-{timestampText}";
        string candidate = Path.Combine(directory, baseName + ".json");

        int suffix = 1;
        while (File.Exists(candidate))
        {
            candidate = Path.Combine(directory, $"{baseName}-{suffix:00}.json");
            suffix++;
        }

        return candidate;
    }

    private static string SanitizeSegment(string value)
    {
        var builder = new StringBuilder(value.Length);
        bool previousWasSeparator = false;

        foreach (char c in value)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                builder.Append(char.ToLowerInvariant(c));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }
}
