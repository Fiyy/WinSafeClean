using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class EvidenceSerializationTests
{
    [Fact]
    public void JsonShouldSerializeEvidenceWithReadableType()
    {
        var report = new ScanReport(
            SchemaVersion: "1.3",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Tools\app.exe",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 10,
                    LastWriteTimeUtc: null,
                    Evidence:
                    [
                        new EvidenceRecord(
                            Type: EvidenceType.ServiceReference,
                            Source: "ExampleService",
                            Confidence: 0.9,
                            Message: "Service binary points to this file.")
                    ],
                    Risk: RiskAssessment.Unknown("No path-level rule matched this item."))
            ]);

        var json = ScanReportJsonSerializer.Serialize(report);

        Assert.Contains(@"""schemaVersion"": ""1.3""", json);
        Assert.Contains(@"""evidence"": [", json);
        Assert.Contains(@"""type"": ""ServiceReference""", json);
        Assert.Contains(@"""source"": ""ExampleService""", json);
        Assert.Contains(@"""confidence"": 0.9", json);
        Assert.Contains("Service binary points to this file.", json);
    }

    [Fact]
    public void MarkdownShouldRenderEvidenceSection()
    {
        var report = new ScanReport(
            SchemaVersion: "1.3",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Tools\app.exe",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 10,
                    LastWriteTimeUtc: null,
                    Evidence:
                    [
                        new EvidenceRecord(
                            Type: EvidenceType.ScheduledTaskReference,
                            Source: "Daily task",
                            Confidence: 0.8,
                            Message: "Task action references this path.")
                    ],
                    Risk: RiskAssessment.Unknown("No path-level rule matched this item."))
            ]);

        var markdown = ScanReportMarkdownSerializer.Serialize(report);

        Assert.Contains("## Evidence", markdown);
        Assert.Contains(@"`C:\Tools\app.exe`: ScheduledTaskReference", markdown);
        Assert.Contains("Daily task", markdown);
        Assert.Contains("Task action references this path.", markdown);
    }
}
