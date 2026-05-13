using System.Text;

namespace WinSafeClean.Ui.ViewModels;

public static class OverviewListExport
{
    public static string CreateScanCsv(IEnumerable<ScanReportOverviewItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return CreateCsv(
            [
                "Path",
                "SizeBytes",
                "Size",
                "Kind",
                "Risk",
                "SuggestedAction",
                "SpaceUseHint",
                "Reasons",
                "Blockers",
                "Evidence"
            ],
            items.Select(item => new[]
            {
                item.Path,
                item.SizeBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                item.SizeDisplay,
                item.ItemKind,
                item.RiskLevel,
                item.SuggestedAction,
                item.SpaceUseHint,
                item.Reasons,
                item.Blockers,
                item.Evidence
            }));
    }

    public static string CreatePlanCsv(IEnumerable<PlanOverviewItemViewModel> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        return CreateCsv(
            [
                "Path",
                "Action",
                "Risk",
                "Reasons",
                "QuarantinePath",
                "RestoreMetadataPath"
            ],
            items.Select(item => new[]
            {
                item.Path,
                item.Action,
                item.RiskLevel,
                item.Reasons,
                item.QuarantinePath ?? string.Empty,
                item.RestoreMetadataPath ?? string.Empty
            }));
    }

    private static string CreateCsv(IReadOnlyList<string> headers, IEnumerable<IReadOnlyList<string>> rows)
    {
        var builder = new StringBuilder();
        AppendRow(builder, headers);

        foreach (var row in rows)
        {
            builder.AppendLine();
            AppendRow(builder, row);
        }

        return builder.ToString();
    }

    private static void AppendRow(StringBuilder builder, IReadOnlyList<string> values)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (index > 0)
            {
                builder.Append(',');
            }

            builder.Append(EscapeCsvValue(values[index]));
        }
    }

    private static string EscapeCsvValue(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        bool requiresQuotes = value.Contains(',')
            || value.Contains('"')
            || value.Contains('\r')
            || value.Contains('\n');

        if (!requiresQuotes)
        {
            return value;
        }

        return '"' + value.Replace("\"", "\"\"", StringComparison.Ordinal) + '"';
    }
}
