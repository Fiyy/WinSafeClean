using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportMarkdownSerializerTests
{
    [Fact]
    public void ShouldRenderHumanReadableRiskReport()
    {
        var report = new ScanReport(
            SchemaVersion: "1.1",
            CreatedAt: new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Windows\Installer",
                    ItemKind: ScanReportItemKind.Directory,
                    SizeBytes: 1024,
                    LastWriteTimeUtc: new DateTimeOffset(2026, 5, 4, 3, 2, 1, TimeSpan.Zero),
                    Risk: PathRiskClassifier.Assess(@"C:\Windows\Installer"))
            ]);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains("# WinSafeClean Scan Report", markdown);
        Assert.Contains("Schema version: `1.1`", markdown);
        Assert.Contains("Created at: `2026-05-05T00:00:00.0000000+00:00`", markdown);
        Assert.Contains("| Path | Type | Size | Last Write (UTC) | Risk | Suggested Action |", markdown);
        Assert.Contains(@"| `C:\Windows\Installer` | Directory | 1 KB | `2026-05-04T03:02:01.0000000+00:00` | Blocked | Keep |", markdown);
        Assert.Contains("Windows Installer cache must not be cleaned manually.", markdown);
    }

    [Fact]
    public void ShouldEscapeMarkdownTablePipesInPaths()
    {
        var report = new ScanReport(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Temp\name|with-pipe",
                    ItemKind: ScanReportItemKind.Unknown,
                    SizeBytes: 0,
                    LastWriteTimeUtc: null,
                    Risk: RiskAssessment.Unknown("No path-level rule matched this item."))
            ]);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains(@"`C:\Temp\name\|with-pipe`", markdown);
    }

    [Fact]
    public void ShouldRenderControlCharactersAsVisibleEscapes()
    {
        var report = new ScanReport(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: "C:\\Temp\\line\nbreak\tfile",
                    ItemKind: ScanReportItemKind.Unknown,
                    SizeBytes: 0,
                    LastWriteTimeUtc: null,
                    Risk: RiskAssessment.Unknown("Reason with\rcontrol"))
            ]);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains(@"C:\Temp\line\nbreak\tfile", markdown);
        Assert.Contains(@"Reason with\rcontrol", markdown);
        Assert.DoesNotContain("line\nbreak", markdown);
        Assert.DoesNotContain("Reason with\rcontrol".Replace(@"\r", "\r"), markdown);
    }

    [Fact]
    public void ShouldRenderMissingLastWriteTimeAsDash()
    {
        var report = new ScanReport(
            SchemaVersion: "1.1",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\missing.tmp",
                    ItemKind: ScanReportItemKind.Unknown,
                    SizeBytes: 0,
                    LastWriteTimeUtc: null,
                    Risk: RiskAssessment.Unknown("Missing path."))
            ]);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains(@"| `C:\missing.tmp` | Unknown | 0 B | - | Unknown | ReportOnly |", markdown);
    }

    [Fact]
    public void ShouldRenderEmptyReportItemsSections()
    {
        var report = new ScanReport(
            SchemaVersion: "1.1",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items: []);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains("## Items", markdown);
        Assert.Contains("| Path | Type | Size | Last Write (UTC) | Risk | Suggested Action |", markdown);
        Assert.Contains("## Reasons", markdown);
        Assert.Contains("## Blockers", markdown);
        Assert.DoesNotContain("| `", markdown);
    }
}
