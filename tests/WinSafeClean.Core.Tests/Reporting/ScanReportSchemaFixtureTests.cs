using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportSchemaFixtureTests
{
    [Fact]
    public void ShouldMatchVersion13JsonFixture()
    {
        var report = new ScanReport(
            SchemaVersion: "1.3",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Users\Alice\AppData\Local\Example\cache.db",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 4096,
                    LastWriteTimeUtc: new DateTimeOffset(2026, 5, 4, 3, 2, 1, TimeSpan.Zero),
                    Evidence:
                    [
                        new EvidenceRecord(
                            Type: EvidenceType.KnownCleanupRule,
                            Source: "CleanerML: example.cache",
                            Confidence: 0.7,
                            Message: "Matched read-only cleanup rule candidate.")
                    ],
                    Risk: new RiskAssessment(
                        Level: RiskLevel.Unknown,
                        Confidence: 0.25,
                        SuggestedAction: SuggestedAction.ReportOnly,
                        Reasons: ["Known cleanup rule evidence is not enough to delete automatically."],
                        Blockers: ["No ownership or active-reference evidence has been evaluated."]))
            ]);

        var json = ScanReportJsonSerializer.Serialize(report);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Reporting", "fixtures", "scan-report-v1.3.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
    }
}
