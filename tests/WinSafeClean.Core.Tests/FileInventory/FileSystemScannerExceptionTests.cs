using System.Security;
using WinSafeClean.Core.FileInventory;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.FileInventory;

public sealed class FileSystemScannerExceptionTests
{
    public static TheoryData<Exception, string> DirectoryEnumerationFailures()
    {
        return new TheoryData<Exception, string>
        {
            { new UnauthorizedAccessException("denied"), "denied" },
            { new PathTooLongException("too long"), "too long" },
            { new IOException("io"), "could not be enumerated" },
            { new SecurityException("policy"), "security policy" }
        };
    }

    public static TheoryData<Exception, string> FileMetadataFailures()
    {
        return new TheoryData<Exception, string>
        {
            { new UnauthorizedAccessException("denied"), "denied" },
            { new PathTooLongException("too long"), "too long" },
            { new IOException("io"), "metadata could not be read" },
            { new SecurityException("policy"), "security policy" }
        };
    }

    [Theory]
    [MemberData(nameof(DirectoryEnumerationFailures))]
    public void ShouldReturnUnknownItemWhenDirectoryCannotBeEnumerated(Exception exception, string expectedReason)
    {
        var fileSystem = new TestFileSystem
        {
            DirectoryExistsFunc = _ => true,
            EnumerateFileSystemEntriesFunc = _ => throw exception
        };

        var items = FileSystemScanner.Scan(
            @"C:\scan-root",
            new FileSystemScanOptions(MaxItems: 100),
            fileSystem);

        var item = Assert.Single(items);
        Assert.Equal(@"C:\scan-root", item.Path);
        Assert.Equal(0, item.SizeBytes);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Equal(SuggestedAction.ReportOnly, item.Risk.SuggestedAction);
        Assert.Contains(item.Risk.Reasons, reason => reason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [MemberData(nameof(FileMetadataFailures))]
    public void ShouldReturnUnknownItemWhenFileMetadataCannotBeRead(Exception exception, string expectedReason)
    {
        var fileSystem = new TestFileSystem
        {
            FileExistsFunc = _ => true,
            GetFileLengthFunc = _ => throw exception
        };

        var items = FileSystemScanner.Scan(
            @"C:\scan-root\sample.log",
            new FileSystemScanOptions(MaxItems: 100),
            fileSystem);

        var item = Assert.Single(items);
        Assert.Equal(@"C:\scan-root\sample.log", item.Path);
        Assert.Equal(0, item.SizeBytes);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Equal(SuggestedAction.ReportOnly, item.Risk.SuggestedAction);
        Assert.Contains(item.Risk.Reasons, reason => reason.Contains(expectedReason, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ShouldReturnUnknownItemWhenPathNormalizationThrowsSupportedFailure()
    {
        var fileSystem = new TestFileSystem
        {
            GetFullPathFunc = _ => throw new NotSupportedException("unsupported path")
        };

        var items = FileSystemScanner.Scan(
            "unsupported:path",
            new FileSystemScanOptions(MaxItems: 100),
            fileSystem);

        var item = Assert.Single(items);
        Assert.Equal("unsupported:path", item.Path);
        Assert.Equal(RiskLevel.Unknown, item.Risk.Level);
        Assert.Contains(item.Risk.Reasons, reason => reason.Contains("invalid", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class TestFileSystem : IFileSystem
    {
        public Func<string, string> GetFullPathFunc { get; init; } = path => path;

        public Func<string, bool> FileExistsFunc { get; init; } = _ => false;

        public Func<string, bool> DirectoryExistsFunc { get; init; } = _ => false;

        public Func<string, IEnumerable<string>> EnumerateFileSystemEntriesFunc { get; init; } = _ => [];

        public Func<string, long> GetFileLengthFunc { get; init; } = _ => 0;

        public string GetFullPath(string path)
        {
            return GetFullPathFunc(path);
        }

        public bool FileExists(string path)
        {
            return FileExistsFunc(path);
        }

        public bool DirectoryExists(string path)
        {
            return DirectoryExistsFunc(path);
        }

        public IEnumerable<string> EnumerateFileSystemEntries(string path)
        {
            return EnumerateFileSystemEntriesFunc(path);
        }

        public long GetFileLength(string path)
        {
            return GetFileLengthFunc(path);
        }
    }
}
