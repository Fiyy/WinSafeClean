using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportPrivacyRedactorTests
{
    [Fact]
    public void ShouldRedactPathsAndTimeMetadataWithoutChangingRiskDecision()
    {
        var report = new ScanReport(
            SchemaVersion: "1.2",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Users\Alice\AppData\Local\Temp\cache.bin",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 128,
                    LastWriteTimeUtc: new DateTimeOffset(2026, 5, 5, 1, 2, 3, TimeSpan.Zero),
                    Risk: RiskAssessment.Unknown(@"Observed C:\Users\Alice\AppData\Local\Temp\cache.bin during scan."))
            ]);

        var redacted = ScanReportPrivacyRedactor.Redact(report);

        Assert.Equal("1.2", redacted.SchemaVersion);
        Assert.Equal(ScanReportPrivacyMode.Redacted, redacted.PrivacyMode);

        var item = Assert.Single(redacted.Items);
        Assert.Equal("[redacted-path-0001]", item.Path);
        Assert.Equal(ScanReportItemKind.File, item.ItemKind);
        Assert.Equal(128, item.SizeBytes);
        Assert.Null(item.LastWriteTimeUtc);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Equal(SuggestedAction.ReportOnly, item.Risk.SuggestedAction);
        Assert.Contains("[redacted-path-0001]", item.Risk.Reasons.Single());
        Assert.DoesNotContain("Alice", item.Risk.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldReuseSameAliasForRepeatedPath()
    {
        var risk = RiskAssessment.Unknown("same path appears twice");
        var report = new ScanReport(
            SchemaVersion: "1.2",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Temp\same.tmp",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 1,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Risk: risk),
                new ScanReportItem(
                    Path: @"C:\Temp\same.tmp",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 2,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Risk: risk)
            ]);

        var redacted = ScanReportPrivacyRedactor.Redact(report);

        Assert.Equal(redacted.Items[0].Path, redacted.Items[1].Path);
    }

    [Fact]
    public void ShouldRedactPathsInsideReasonsAndBlockers()
    {
        const string path = @"C:\Users\Alice\AppData\Local\Temp\cache.bin";
        var risk = new RiskAssessment(
            RiskLevel.Blocked,
            1.0,
            SuggestedAction.Keep,
            [$"Reason mentions {path} and parent C:\\Users\\Alice."],
            [$"Blocker mentions {path}."]);
        var report = new ScanReport(
            SchemaVersion: "1.2",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: path,
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 1,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Risk: risk)
            ]);

        var redacted = ScanReportPrivacyRedactor.Redact(report);
        var item = Assert.Single(redacted.Items);

        Assert.Contains("[redacted-path-0001]", item.Risk.Reasons.Single());
        Assert.Contains("[redacted-path-0001]", item.Risk.Blockers.Single());
        Assert.Contains("[redacted-path]", item.Risk.Reasons.Single());
        Assert.DoesNotContain("Alice", item.Risk.Reasons.Single(), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alice", item.Risk.Blockers.Single(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldRedactPathsInsideEvidence()
    {
        const string path = @"C:\Users\Alice\AppData\Local\Vendor\app.exe";
        var report = new ScanReport(
            SchemaVersion: "1.3",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: path,
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 1,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Evidence:
                    [
                        new EvidenceRecord(
                            Type: EvidenceType.ServiceReference,
                            Source: @"ImagePath C:\Users\Alice\AppData\Local\Vendor\app.exe",
                            Confidence: 0.9,
                            Message: @"Service references C:\Users\Alice\AppData\Local\Vendor\app.exe")
                    ],
                    Risk: RiskAssessment.Unknown("No path-level rule matched this item."))
            ]);

        var redacted = ScanReportPrivacyRedactor.Redact(report);
        var evidence = Assert.Single(Assert.Single(redacted.Items).Evidence);

        Assert.Contains("[redacted-path-0001]", evidence.Source);
        Assert.Contains("[redacted-path-0001]", evidence.Message);
        Assert.DoesNotContain("Alice", evidence.Source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Alice", evidence.Message, StringComparison.OrdinalIgnoreCase);
    }
}
