using WinSafeClean.Ui.ViewModels;

namespace WinSafeClean.Ui.Tests;

public sealed class OverviewListExportTests
{
    [Fact]
    public void CreateScanCsv_WritesHeadersAndEscapesValues()
    {
        var items = new[]
        {
            new ScanReportOverviewItemViewModel(
                Path: @"C:\Temp\cache,one.bin",
                ItemKind: "File",
                SizeBytes: 42,
                SizeDisplay: "42 B",
                LastWriteTimeDisplay: "2026-05-10 08:00:00Z",
                RiskLevel: "SafeCandidate",
                SuggestedAction: "ReportOnly",
                SpaceUseHint: "Cache-like item.",
                Reasons: "Known \"cache\" rule.",
                Blockers: "",
                Evidence: "Rule: CleanerML - line one\nline two")
        };

        string csv = OverviewListExport.CreateScanCsv(items);

        string[] lines = csv.Split(Environment.NewLine);
        Assert.Equal("Path,SizeBytes,Size,Kind,Risk,SuggestedAction,SpaceUseHint,Reasons,Blockers,Evidence", lines[0]);
        Assert.Contains(@"""C:\Temp\cache,one.bin""", lines[1]);
        Assert.Contains(@"""Known """"cache"""" rule.""", lines[1]);
        Assert.Contains(@"""Rule: CleanerML - line one", csv);
    }

    [Fact]
    public void CreatePlanCsv_WritesQuarantinePreviewColumns()
    {
        var items = new[]
        {
            new PlanOverviewItemViewModel(
                Path: @"C:\Temp\cache.bin",
                Action: "ReviewForQuarantine",
                RiskLevel: "SafeCandidate",
                Reasons: "Known cache rule.",
                QuarantinePath: @"C:\Quarantine\cache.bin",
                RestoreMetadataPath: @"C:\Quarantine\cache.bin.restore.json")
        };

        string csv = OverviewListExport.CreatePlanCsv(items);

        string[] lines = csv.Split(Environment.NewLine);
        Assert.Equal("Path,Action,Risk,Reasons,QuarantinePath,RestoreMetadataPath", lines[0]);
        Assert.Equal(
            @"C:\Temp\cache.bin,ReviewForQuarantine,SafeCandidate,Known cache rule.,C:\Quarantine\cache.bin,C:\Quarantine\cache.bin.restore.json",
            lines[1]);
    }
}
