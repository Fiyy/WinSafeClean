using WinSafeClean.Core.FileInventory;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.FileInventory;

public sealed class FileSystemScannerTests
{
    [Fact]
    public void ShouldScanSingleFileAsOneReportItem()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("sample.txt", "hello");

        var items = FileSystemScanner.Scan(filePath, new FileSystemScanOptions(MaxItems: 100));

        var item = Assert.Single(items);
        Assert.Equal(filePath, item.Path);
        Assert.Equal(5, item.SizeBytes);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Equal(SuggestedAction.ReportOnly, item.Risk.SuggestedAction);
    }

    [Fact]
    public void ShouldScanDirectoryChildrenWithoutRecursingByDefault()
    {
        using var sandbox = TemporarySandbox.Create();
        var alpha = sandbox.WriteFile("alpha.txt", "a");
        var nestedDirectory = sandbox.CreateDirectory("nested");
        sandbox.WriteFile(Path.Combine("nested", "hidden.txt"), "hidden");

        var items = FileSystemScanner.Scan(sandbox.RootPath, new FileSystemScanOptions(MaxItems: 100));

        Assert.Collection(
            items,
            first =>
            {
                Assert.Equal(alpha, first.Path);
                Assert.Equal(1, first.SizeBytes);
            },
            second =>
            {
                Assert.Equal(nestedDirectory, second.Path);
                Assert.Equal(0, second.SizeBytes);
            });
    }

    [Fact]
    public void ShouldLimitDirectoryResultsByMaxItems()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteFile("a.txt", "a");
        sandbox.WriteFile("b.txt", "b");
        sandbox.WriteFile("c.txt", "c");

        var items = FileSystemScanner.Scan(sandbox.RootPath, new FileSystemScanOptions(MaxItems: 2));

        Assert.Equal(2, items.Count);
        Assert.EndsWith("a.txt", items[0].Path);
        Assert.EndsWith("b.txt", items[1].Path);
    }

    [Fact]
    public void ShouldReturnUnknownItemForMissingPath()
    {
        using var sandbox = TemporarySandbox.Create();
        var missingPath = Path.Combine(sandbox.RootPath, "missing.bin");

        var items = FileSystemScanner.Scan(missingPath, new FileSystemScanOptions(MaxItems: 100));

        var item = Assert.Single(items);
        Assert.Equal(missingPath, item.Path);
        Assert.Equal(0, item.SizeBytes);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Contains(item.Risk.Reasons, reason => reason.Contains("does not exist", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShouldPreserveBlockedRiskForMissingProtectedPath()
    {
        var items = FileSystemScanner.Scan(
            @"\\?\C:\Windows\Installer\missing.msi",
            new FileSystemScanOptions(MaxItems: 100));

        var item = Assert.Single(items);
        Assert.Equal(RiskLevel.Blocked, item.Risk.Level);
        Assert.Equal(SuggestedAction.Keep, item.Risk.SuggestedAction);
    }

    [Fact]
    public void ShouldReturnUnknownItemForInvalidPathSyntax()
    {
        var items = FileSystemScanner.Scan("bad\0path", new FileSystemScanOptions(MaxItems: 100));

        var item = Assert.Single(items);
        Assert.Equal("bad\0path", item.Path);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Contains(item.Risk.Reasons, reason => reason.Contains("invalid", StringComparison.OrdinalIgnoreCase));
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Tests", Path.GetRandomFileName());
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

        public string CreateDirectory(string relativePath)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(path);
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
