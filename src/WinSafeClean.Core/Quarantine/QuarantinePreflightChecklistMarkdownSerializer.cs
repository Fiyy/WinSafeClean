using System.Text;

namespace WinSafeClean.Core.Quarantine;

public static class QuarantinePreflightChecklistMarkdownSerializer
{
    public static string Serialize(QuarantinePreflightChecklist checklist)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        var builder = new StringBuilder();
        builder.AppendLine("# WinSafeClean Preflight Checklist");
        builder.AppendLine();
        builder.AppendLine($"Schema version: `{EscapeInlineCode(checklist.SchemaVersion)}`");
        builder.AppendLine($"Created at: `{checklist.CreatedAt:O}`");
        builder.AppendLine($"Is executable: `{checklist.IsExecutable}`");
        builder.AppendLine();
        builder.AppendLine("| Code | Status | Message |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var check in checklist.Checks)
        {
            builder.AppendLine($"| `{EscapeTableCell(EscapeInlineCode(check.Code))}` | `{check.Status}` | {EscapeTableCell(SanitizeMarkdownText(check.Message))} |");
        }

        return builder.ToString();
    }

    private static string EscapeInlineCode(string value)
    {
        return SanitizeMarkdownText(value).Replace("`", "\\`", StringComparison.Ordinal);
    }

    private static string EscapeTableCell(string value)
    {
        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }

    private static string SanitizeMarkdownText(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            switch (character)
            {
                case '\r':
                    builder.Append(@"\r");
                    break;
                case '\n':
                    builder.Append(@"\n");
                    break;
                case '\t':
                    builder.Append(@"\t");
                    break;
                default:
                    if (char.IsControl(character))
                    {
                        builder.Append(@"\u");
                        builder.Append(((int)character).ToString("X4"));
                    }
                    else
                    {
                        builder.Append(character);
                    }

                    break;
            }
        }

        return builder.ToString();
    }
}
