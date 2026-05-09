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

        Assert.Equal(4, viewModel.TotalItems);
        Assert.Equal("1.3", viewModel.SchemaVersion);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "Blocked" && item.Count == 1);
        Assert.Contains(viewModel.RiskSummaries, item => item.Label == "LowRisk" && item.Count == 1);
        Assert.Contains(viewModel.ItemKindSummaries, item => item.Label == "File" && item.Count == 2);
        Assert.Contains(viewModel.ItemKindSummaries, item => item.Label == "Directory" && item.Count == 2);
    }

    [Fact]
    public void ShouldExposeReadableSizeSummaries()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        Assert.Equal(16_778_752, viewModel.TotalSizeBytes);
        Assert.Equal("16.0 MB", viewModel.TotalSizeDisplay);

        var item = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Temp\cache.tmp"));

        Assert.Equal(1536, item.SizeBytes);
        Assert.Equal("1.5 KB", item.SizeDisplay);
    }

    [Fact]
    public void ShouldSortScanItemsBySizeDescendingForSpaceReview()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        Assert.Equal(
            [@"C:\Temp\unknown.bin", @"C:\Users\Alice\AppData\Local\Example\Cache", @"C:\Temp\cache.tmp", @"C:\Windows\System32"],
            viewModel.Items.Select(item => item.Path));
    }

    [Fact]
    public void ShouldExposeLargestNonEmptyItems()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        Assert.True(viewModel.HasLargestItems);
        Assert.Equal([@"C:\Temp\unknown.bin", @"C:\Users\Alice\AppData\Local\Example\Cache", @"C:\Temp\cache.tmp"], viewModel.LargestItems.Select(item => item.Path));
    }

    [Fact]
    public void ShouldExposeLargestDirectories()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        Assert.True(viewModel.HasLargestDirectories);
        Assert.Equal([@"C:\Users\Alice\AppData\Local\Example\Cache"], viewModel.LargestDirectories.Select(item => item.Path));
    }

    [Fact]
    public void ShouldExposeCommonSpaceUseHintsWithoutChangingRisk()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        var cacheDirectory = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Users\Alice\AppData\Local\Example\Cache"));
        var protectedDirectory = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Windows\System32"));
        var tempFile = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Temp\cache.tmp"));

        Assert.Contains("Cache-like", cacheDirectory.SpaceUseHint);
        Assert.Contains("Protected Windows area", protectedDirectory.SpaceUseHint);
        Assert.Contains("Temporary", tempFile.SpaceUseHint);
        Assert.Equal("MediumRisk", cacheDirectory.RiskLevel);
    }

    [Fact]
    public void ShouldExposeReadableLastWriteTimeForDetails()
    {
        var viewModel = ScanReportOverviewViewModel.FromReport(CreateReport());

        var cache = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Temp\cache.tmp"));
        var unknown = Assert.Single(viewModel.Items.Where(item => item.Path == @"C:\Temp\unknown.bin"));

        Assert.Equal("1970-01-01 00:00:00 UTC", cache.LastWriteTimeDisplay);
        Assert.Equal("-", unknown.LastWriteTimeDisplay);
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

    [Fact]
    public void EmptyScanOverviewShouldExposeEmptyState()
    {
        Assert.False(ScanReportOverviewViewModel.Empty.HasItems);
        Assert.False(ScanReportOverviewViewModel.Empty.HasLargestItems);
        Assert.False(ScanReportOverviewViewModel.Empty.HasLargestDirectories);
        Assert.Equal("No scan report loaded.", ScanReportOverviewViewModel.Empty.EmptyStateMessage);
        Assert.Equal("Select a scan item to view details.", ScanReportOverviewViewModel.Empty.SelectionEmptyStateMessage);
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
                    SizeBytes: 1536,
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
                    Path: @"C:\Users\Alice\AppData\Local\Example\Cache",
                    ItemKind: ScanReportItemKind.Directory,
                    SizeBytes: 6_291_456,
                    LastWriteTimeUtc: DateTimeOffset.UnixEpoch,
                    Evidence: [],
                    Risk: new RiskAssessment(
                        Level: RiskLevel.MediumRisk,
                        Confidence: 0.4,
                        SuggestedAction: SuggestedAction.ReportOnly,
                        Reasons: ["Cache-like directory; ownership still needs review."],
                        Blockers: [])),
                new ScanReportItem(
                    Path: @"C:\Temp\unknown.bin",
                    ItemKind: ScanReportItemKind.File,
                    SizeBytes: 10_485_760,
                    LastWriteTimeUtc: null,
                    Evidence: [],
                    Risk: RiskAssessment.Unknown("Unknown file."))
            ]);
    }
}
