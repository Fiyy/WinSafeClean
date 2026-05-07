using System.Text;

namespace WinSafeClean.Core.Quarantine;

public static class RestoreExecutionResultMarkdownSerializer
{
    public static string Serialize(RestoreExecutionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var builder = new StringBuilder();
        builder.AppendLine("# WinSafeClean Restore Result");
        builder.AppendLine();
        builder.AppendLine($"Succeeded: `{result.Succeeded}`");
        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            builder.AppendLine($"Error: {SanitizeMarkdownText(result.ErrorMessage)}");
        }

        if (!string.IsNullOrWhiteSpace(result.WarningMessage))
        {
            builder.AppendLine($"Warning: {SanitizeMarkdownText(result.WarningMessage)}");
        }

        return builder.ToString();
    }

    private static string SanitizeMarkdownText(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            if (char.IsControl(character))
            {
                builder.Append(@"\u");
                builder.Append(((int)character).ToString("X4"));
            }
            else
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }
}
