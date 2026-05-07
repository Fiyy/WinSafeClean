using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;
using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class ScanReportOverviewViewModelTests
{
    [Fact]
    public void ShouldSummarizeScanReportRiskLevelsAndItemKinds()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        Assert.Equal(3, viewModel.TotalItems);
        Assert.Equal("1.3", viewModel.SchemaVersion);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "Blocked" && item.Count == 1);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "LowRisk" && item.Count == 1);
        Assert.Contains(viewModel.ItemKindSummaries, item => item.Label == "File" && item.Count == 2);
        Assert.Contains(viewModel.ItemKindSummaries, item => item.Label == "Directory" && item.Count == 1);
    }

    [Fact]
    public void ShouldExposeEvidenceAndReasonsForReportItems()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        var item = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Temp\cache.tmp"));

        Assert.Equal("LowRisk", item.RiskLevel);
        Assert.Equal("ReportOnly", item.SuggestedAction);
        Assert.Contains("Known cleanup rule matched", item.Reasons);
        Assert.Contains("CleanerML", item.Evidence);
    }

    private static ScanReport CreateReport()
    {
        return new ScanReport(
            SchemaVersion: "1.3",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new ScanReportItem(
                    Path: @"C:\Windows\System32",
                    ItemKind: ScanReportItemKind.Directory,
                    SizeBytes: 0,
                    LastWriteTimeUtc: null,
                    Evidence: [],
                    Risk: RiskAssessment.Blocked(SuggestedAction.Keep, "Protected Windows path.")),
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
                            Message: "CleanerML file candidate matched.")
                    ],
                    Risk: new RiskAssessment(
                        Level: RiskLevel.LowRisk,
                        Confidence: 0.7,
                        SuggestedAction: SuggestedAction.ReportOnly,
                        Reasons: ["Known cleanup rule matched."],
                        Blockers: [])),
                new ScanReportItem(
                    Path: @"C:\Temp\unknown.bin",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 10,
                    LastWriteTimeUtc: null,
                    Evidence: [],
                    Risk: RiskAssessment.Unknown("Unknown file."))
            ]);
    }
}
