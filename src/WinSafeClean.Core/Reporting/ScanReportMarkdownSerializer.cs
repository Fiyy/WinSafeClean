using System.Globalization;
using System.Text;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportMarkdownSerializer
{
    public static string Serialize(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();

        builder.AppendLine("# WinSafeClean Scan Report");
        builder.AppendLine();
        builder.AppendLine($"Schema version: `{EscapeInlineCode(report.SchemaVersion)}`");
        builder.AppendLine($"Privacy mode: `{report.PrivacyMode}`");
        builder.AppendLine($"Created at: `{report.CreatedAt:O}`");
        builder.AppendLine();
        builder.AppendLine("## Items");
        builder.AppendLine();
        builder.AppendLine("| Path | Type | Size | Last Write (UTC) | Risk | Suggested Action |");
        builder.AppendLine("| --- | --- | ---: | --- | --- | --- |");

        foreach (var item in report.Items)
        {
            var lastWriteTime = item.LastWriteTimeUtc.HasValue
                ? $"`{item.LastWriteTimeUtc.Value:O}`"
                : "-";

            builder.AppendLine(
                $"| `{EscapeTableCell(EscapeInlineCode(item.Path))}` | {item.ItemKind} | {FormatBytes(item.SizeBytes)} | {lastWriteTime} | {item.Risk.Level} | {item.Risk.SuggestedAction} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Reasons");
        builder.AppendLine();

        foreach (var item in report.Items)
        {
            foreach (var reason in item.Risk.Reasons)
            {
                builder.AppendLine($"- `{EscapeInlineCode(item.Path)}`: {SanitizeMarkdownText(reason)}");
            }
        }

        builder.AppendLine();
        builder.AppendLine("## Blockers");
        builder.AppendLine();

        foreach (var item in report.Items)
        {
            foreach (var blocker in item.Risk.Blockers)
            {
                builder.AppendLine($"- `{EscapeInlineCode(item.Path)}`: {SanitizeMarkdownText(blocker)}");
            }
        }

        return builder.ToString();
    }

    private static string FormatBytes(long sizeBytes)
    {
        if (sizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must not be negative.");
        }

        if (sizeBytes < 1024)
        {
            return sizeBytes.ToString(CultureInfo.InvariantCulture) + " B";
        }

        var kibibytes = sizeBytes / 1024.0;
        return kibibytes.ToString("0.#", CultureInfo.InvariantCulture) + " KB";
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
                        builder.Append(((int)character).ToString("X4", CultureInfo.InvariantCulture));
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
