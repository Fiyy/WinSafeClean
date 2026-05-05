using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportJsonSerializerTests
{
    [Fact]
    public void ShouldSerializeRiskReportWithReadableEnumValues()
    {
        var report = new ScanReport(
            SchemaVersion: "1.0",
            CreatedAt: new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero),
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Windows\Installer",
                    SizeBytes: 1024,
                    Risk: PathRiskClassifier.Assess(@"C:\Windows\Installer"))
            ]);

        var json = ScanReportJsonSerializer.Serialize(report);

        Assert.Contains(@"""schemaVersion"": ""1.0""", json);
        Assert.Contains(@"""path"": ""C:\\Windows\\Installer""", json);
        Assert.Contains(@"""level"": ""Blocked""", json);
        Assert.Contains(@"""suggestedAction"": ""Keep""", json);
        Assert.Contains("Windows Installer cache", json);
    }
}
