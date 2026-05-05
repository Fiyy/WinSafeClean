using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class RunningProcessEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnRunningProcessEvidenceWhenMainModulePathMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var processPath = sandbox.WriteFile("app.exe");
        var provider = new RunningProcessEvidenceProvider(new StubWindowsProcessSource(
        [
            new WindowsProcessRecord(ProcessId: 1234, ProcessName: "example", MainModuleFilePath: processPath)
        ]));

        var evidence = provider.CollectEvidence(processPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.RunningProcessReference, item.Type);
        Assert.Equal("example (PID 1234)", item.Source);
        Assert.Equal(1.0, item.Confidence);
        Assert.Contains("Running process", item.Message);
        Assert.Contains(processPath, item.Message);
    }

    [Fact]
    public void ShouldReturnEvidenceForEachMatchingProcess()
    {
        using var sandbox = TemporarySandbox.Create();
        var processPath = sandbox.WriteFile("app.exe");
        var provider = new RunningProcessEvidenceProvider(new StubWindowsProcessSource(
        [
            new WindowsProcessRecord(ProcessId: 1234, ProcessName: "example", MainModuleFilePath: processPath),
            new WindowsProcessRecord(ProcessId: 5678, ProcessName: "example-helper", MainModuleFilePath: processPath)
        ]));

        var evidence = provider.CollectEvidence(processPath);

        Assert.Equal(2, evidence.Count);
    }

    [Fact]
    public void ShouldSkipProcessesWithoutReadableMainModulePath()
    {
        using var sandbox = TemporarySandbox.Create();
        var processPath = sandbox.WriteFile("app.exe");
        var provider = new RunningProcessEvidenceProvider(new StubWindowsProcessSource(
        [
            new WindowsProcessRecord(ProcessId: 1234, ProcessName: "example", MainModuleFilePath: null)
        ]));

        var evidence = provider.CollectEvidence(processPath);

        Assert.Empty(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenNoProcessMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile("target.exe");
        var otherPath = sandbox.WriteFile("other.exe");
        var provider = new RunningProcessEvidenceProvider(new StubWindowsProcessSource(
        [
            new WindowsProcessRecord(ProcessId: 1234, ProcessName: "other", MainModuleFilePath: otherPath)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsProcessSource(IReadOnlyList<WindowsProcessRecord> processes) : IWindowsProcessSource
    {
        public IReadOnlyList<WindowsProcessRecord> GetProcesses()
        {
            return processes;
        }
    }

    private sealed class TemporarySandbox : IDisposable
    {
        private TemporarySandbox(string rootPath)
        {
            RootPath = rootPath;
        }

        private string RootPath { get; }

        public static TemporarySandbox Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
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
}
