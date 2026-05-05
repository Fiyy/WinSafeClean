using System.Text.RegularExpressions;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportPrivacyRedactor
{
    private static readonly Regex WindowsPathPattern = new(
        @"(?i)(?:[A-Z]:\\[^\s\]\)\}""'<>|]+|\\\\[^\\\s]+\\[^\s\]\)\}""'<>|]+)",
        RegexOptions.Compiled);

    public static ScanReport Redact(ScanReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var aliases = CreateAliases(report.Items);
        var redactedItems = report.Items
            .Select(item => RedactItem(item, aliases))
            .ToList();

        return report with
        {
            PrivacyMode = ScanReportPrivacyMode.Redacted,
            Items = redactedItems
        };
    }

    private static Dictionary<string, string> CreateAliases(IReadOnlyList<ScanReportItem> items)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in items)
        {
            if (aliases.ContainsKey(item.Path))
            {
                continue;
            }

            aliases[item.Path] = $"[redacted-path-{aliases.Count + 1:0000}]";
        }

        return aliases;
    }

    private static ScanReportItem RedactItem(ScanReportItem item, Dictionary<string, string> aliases)
    {
        return item with
        {
            Path = aliases[item.Path],
            LastWriteTimeUtc = null,
            Risk = RedactRisk(item.Risk, aliases)
        };
    }

    private static RiskAssessment RedactRisk(RiskAssessment risk, Dictionary<string, string> aliases)
    {
        return risk with
        {
            Reasons = risk.Reasons
                .Select(reason => RedactKnownPaths(reason, aliases))
                .ToList(),
            Blockers = risk.Blockers
                .Select(blocker => RedactKnownPaths(blocker, aliases))
                .ToList()
        };
    }

    private static string RedactKnownPaths(string value, Dictionary<string, string> aliases)
    {
        var redacted = value;

        foreach (var alias in aliases.OrderByDescending(alias => alias.Key.Length))
        {
            redacted = redacted.Replace(alias.Key, alias.Value, StringComparison.OrdinalIgnoreCase);
        }

        return WindowsPathPattern.Replace(redacted, "[redacted-path]");
    }
}
