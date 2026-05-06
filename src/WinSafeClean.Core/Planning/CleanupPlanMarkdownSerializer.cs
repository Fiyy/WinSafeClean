using System.Text;

namespace WinSafeClean.Core.Planning;

public static class CleanupPlanMarkdownSerializer
{
    public static string Serialize(CleanupPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var builder = new StringBuilder();
        builder.AppendLine("# WinSafeClean Cleanup Plan");
        builder.AppendLine();
        builder.AppendLine($"Schema version: `{EscapeInlineCode(plan.SchemaVersion)}`");
        builder.AppendLine($"Created at: `{plan.CreatedAt:O}`");
        if (!string.IsNullOrWhiteSpace(plan.QuarantineRoot))
        {
            builder.AppendLine($"Quarantine root: `{EscapeInlineCode(plan.QuarantineRoot)}`");
        }

        builder.AppendLine();
        builder.AppendLine("## Items");
        builder.AppendLine();
        builder.AppendLine("| Path | Action | Risk |");
        builder.AppendLine("| --- | --- | --- |");

        foreach (var item in plan.Items)
        {
            builder.AppendLine($"| `{EscapeTableCell(EscapeInlineCode(item.Path))}` | `{item.Action}` | {item.RiskLevel} |");
        }

        builder.AppendLine();
        builder.AppendLine("## Reasons");
        builder.AppendLine();

        foreach (var item in plan.Items)
        {
            foreach (var reason in item.Reasons)
            {
                builder.AppendLine($"- `{EscapeInlineCode(item.Path)}`: {SanitizeMarkdownText(reason)}");
            }
        }

        var quarantinePreviewItems = plan.Items
            .Where(item => item.QuarantinePreview is not null)
            .ToArray();
        if (quarantinePreviewItems.Length > 0)
        {
            builder.AppendLine();
            builder.AppendLine("## Quarantine Preview");
            builder.AppendLine();
            builder.AppendLine("| Source | Proposed quarantine path | Restore metadata |");
            builder.AppendLine("| --- | --- | --- |");

            foreach (var item in quarantinePreviewItems)
            {
                var preview = item.QuarantinePreview!;
                builder.AppendLine(
                    $"| `{EscapeTableCell(EscapeInlineCode(preview.OriginalPath))}` | `{EscapeTableCell(EscapeInlineCode(preview.ProposedQuarantinePath))}` | `{EscapeTableCell(EscapeInlineCode(preview.RestoreMetadataPath))}` |");
            }

            builder.AppendLine();
            builder.AppendLine("## Quarantine Warnings");
            builder.AppendLine();

            foreach (var item in quarantinePreviewItems)
            {
                var preview = item.QuarantinePreview!;
                foreach (var warning in preview.Warnings)
                {
                    builder.AppendLine($"- `{EscapeInlineCode(preview.OriginalPath)}`: {SanitizeMarkdownText(warning)}");
                }
            }
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
