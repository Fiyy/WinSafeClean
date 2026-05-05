using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportJsonSerializerTests
{
    [Fact]
    public void ShouldSerializeRiskReportWithReadableEnumValues()
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

        var json = ScanReportJsonSerializer.Serialize(report);

        Assert.Contains(@"""schemaVersion"": ""1.1""", json);
        Assert.Contains(@"""path"": ""C:\\Windows\\Installer""", json);
        Assert.Contains(@"""itemKind"": ""Directory""", json);
        Assert.Contains(@"""lastWriteTimeUtc"": ""2026-05-04T03:02:01+00:00""", json);
        Assert.Contains(@"""level"": ""Blocked""", json);
        Assert.Contains(@"""suggestedAction"": ""Keep""", json);
        Assert.Contains("Windows Installer cache", json);
    }

    [Fact]
    public void ShouldSerializeNullLastWriteTimeForUnknownItems()
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
                    Risk: RiskAssessment.Unknown("Path does not exist."))
            ]);

        var json = ScanReportJsonSerializer.Serialize(report);

        Assert.Contains(@"""itemKind"": ""Unknown""", json);
        Assert.Contains(@"""lastWriteTimeUtc"": null", json);
    }
}
