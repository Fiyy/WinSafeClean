using System.Text.Json;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportJsonSerializerTests
{
    [Fact]
    public void ShouldSerializeRiskReportWithReadableEnumValues()
    {
        var report = new ScanReport(
            SchemaVersion: "1.2",
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

        Assert.Contains(@"""schemaVersion"": ""1.2""", json);
        Assert.Contains(@"""privacyMode"": ""Full""", json);
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
            SchemaVersion: "1.2",
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

    [Fact]
    public void ShouldSerializeEmptyReportItems()
    {
        var report = new ScanReport(
            SchemaVersion: "1.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items: []);

        var json = ScanReportJsonSerializer.Serialize(report);

        using var document = JsonDocument.Parse(json);
        Assert.Equal("1.2", document.RootElement.GetProperty("schemaVersion").GetString());
        Assert.Equal("Full", document.RootElement.GetProperty("privacyMode").GetString());
        Assert.Equal(0, document.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public void ShouldDeserializeScanReportJsonWithReadableEnumValues()
    {
        var report = new ScanReport(
            SchemaVersion: "1.3",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Temp\cache.tmp",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 5,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Evidence:
                    [
                        new EvidenceRecord(
                            Type: EvidenceType.KnownCleanupRule,
                            Source: "CleanerML: example.cache",
                            Confidence: 0.6,
                            Message: "Known cache candidate.")
                    ],
                    Risk: new RiskAssessment(
                        Level: RiskLevel.LowRisk,
                        Confidence: 0.7,
                        SuggestedAction: SuggestedAction.ReportOnly,
                        Reasons: ["Known cleanup rule matched."],
                        Blockers: []))
            ]);

        var json = ScanReportJsonSerializer.Serialize(report);

        var deserialized = ScanReportJsonSerializer.Deserialize(json);

        Assert.Equal("1.3", deserialized.SchemaVersion);
        var item = Assert.Single(deserialized.Items);
        Assert.Equal(ScanReportItemKind.File, item.ItemKind);
        Assert.Equal(RiskLevel.LowRisk, item.Risk.Level);
        Assert.Equal(EvidenceType.KnownCleanupRule, Assert.Single(item.Evidence).Type);
    }
}
