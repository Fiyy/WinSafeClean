using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class PathEnvironmentEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnPathEnvironmentEvidenceWhenDirectoryMatchesPathEntry()
    {
        using var sandbox = TemporarySandbox.Create();
        var toolsDirectory = sandbox.CreateDirectory("tools");
        var provider = new PathEnvironmentEvidenceProvider(new StubWindowsPathEnvironmentSource(
        [
            new WindowsPathEnvironmentRecord("Process", toolsDirectory)
        ]));

        var evidence = provider.CollectEvidence(toolsDirectory);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.PathEnvironmentReference, item.Type);
        Assert.Equal("Process PATH", item.Source);
        Assert.Equal(0.75, item.Confidence);
        Assert.Contains(toolsDirectory, item.Message);
    }

    [Fact]
    public void ShouldReturnPathEnvironmentEvidenceWhenFileParentDirectoryMatchesPathEntry()
    {
        using var sandbox = TemporarySandbox.Create();
        var toolPath = sandbox.WriteFile(@"tools\example.exe");
        var toolsDirectory = Path.GetDirectoryName(toolPath)!;
        var provider = new PathEnvironmentEvidenceProvider(new StubWindowsPathEnvironmentSource(
        [
            new WindowsPathEnvironmentRecord("User", toolsDirectory)
        ]));

        var evidence = provider.CollectEvidence(toolPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.PathEnvironmentReference, item.Type);
        Assert.Equal("User PATH", item.Source);
        Assert.Contains("parent directory is listed", item.Message);
    }

    [Fact]
    public void ShouldExpandEnvironmentVariablesAndTrimQuotesInPathEntries()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_PATH_ROOT", sandbox.RootPath);
        var toolPath = sandbox.WriteFile(@"bin\example.exe");
        var provider = new PathEnvironmentEvidenceProvider(new StubWindowsPathEnvironmentSource(
        [
            new WindowsPathEnvironmentRecord("Machine", @"""%WINSAFECLEAN_PATH_ROOT%\bin""")
        ]));

        var evidence = provider.CollectEvidence(toolPath);

        var item = Assert.Single(evidence);
        Assert.Equal("Machine PATH", item.Source);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenPathDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile(@"app\example.exe");
        var otherDirectory = sandbox.CreateDirectory("tools");
        var provider = new PathEnvironmentEvidenceProvider(new StubWindowsPathEnvironmentSource(
        [
            new WindowsPathEnvironmentRecord("Process", otherDirectory)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsPathEnvironmentSource(IReadOnlyList<WindowsPathEnvironmentRecord> records)
        : IWindowsPathEnvironmentSource
    {
        public IReadOnlyList<WindowsPathEnvironmentRecord> GetPathEnvironmentRecords()
        {
            return records;
        }
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.Path.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string CreateDirectory(string relativePath)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string WriteFile(string relativePath)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
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

    private sealed class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string name;
        private readonly string? previousValue;

        private TemporaryEnvironmentVariable(string name, string value)
        {
            this.name = name;
            previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public static TemporaryEnvironmentVariable Set(string name, string value)
        {
            return new TemporaryEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}
