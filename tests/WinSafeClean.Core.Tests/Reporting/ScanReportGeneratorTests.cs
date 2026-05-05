using WinSafeClean.Core.FileInventory;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Reporting;

public sealed class ScanReportGeneratorTests
{
    [Fact]
    public void ShouldGenerateReportFromFileSystemScan()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("sample.txt", "hello");
        var createdAt = new DateTimeOffset(2026, 5, 5, 1, 2, 3, TimeSpan.Zero);

        var expectedLastWriteTime = new DateTimeOffset(File.GetLastWriteTimeUtc(filePath));

        var report = ScanReportGenerator.Generate(
            filePath,
            new FileSystemScanOptions(MaxItems: 100),
            createdAt);

        Assert.Equal("1.1", report.SchemaVersion);
        Assert.Equal(createdAt, report.CreatedAt);

        var item = Assert.Single(report.Items);
        Assert.Equal(filePath, item.Path);
        Assert.Equal(ScanReportItemKind.File, item.ItemKind);
        Assert.Equal(5, item.SizeBytes);
        Assert.Equal(expectedLastWriteTime, item.LastWriteTimeUtc);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Equal(SuggestedAction.ReportOnly, item.Risk.SuggestedAction);
    }

    [Fact]
    public void ShouldHonorScanOptionsWhenGeneratingReport()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteFile("a.txt", "a");
        sandbox.WriteFile("b.txt", "b");
        sandbox.WriteFile("c.txt", "c");

        var report = ScanReportGenerator.Generate(
            sandbox.RootPath,
            new FileSystemScanOptions(MaxItems: 2),
            DateTimeOffset.UnixEpoch);

        Assert.Equal(2, report.Items.Count);
    }

    private sealed class TemporarySandbox : IDisposable
    {
        private TemporarySandbox(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public static TemporarySandbox Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.ReportGenerator.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string WriteFile(string relativePath, string content)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
