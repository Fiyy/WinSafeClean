using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class ScheduledTaskEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnTaskEvidenceWhenQuotedCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var taskPath = sandbox.WriteFile("task.exe");
        var provider = new ScheduledTaskEvidenceProvider(new StubWindowsScheduledTaskSource(
        [
            new WindowsScheduledTaskRecord(
                Path: @"\ExampleTask",
                Uri: @"\ExampleTask",
                Actions:
                [
                    new WindowsScheduledTaskActionRecord($"\"{taskPath}\"", "--run", null)
                ])
        ]));

        var evidence = provider.CollectEvidence(taskPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.ScheduledTaskReference, item.Type);
        Assert.Equal(@"\ExampleTask", item.Source);
        Assert.Equal(0.9, item.Confidence);
        Assert.Contains("Scheduled task action", item.Message);
        Assert.Contains(taskPath, item.Message);
        Assert.Contains("--run", item.Message);
    }

    [Fact]
    public void ShouldReturnTaskEvidenceWhenUnquotedCommandWithArgumentsMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var taskPath = sandbox.WriteFile("task.exe");
        var provider = new ScheduledTaskEvidenceProvider(new StubWindowsScheduledTaskSource(
        [
            new WindowsScheduledTaskRecord(
                Path: @"\ExampleTask",
                Uri: null,
                Actions:
                [
                    new WindowsScheduledTaskActionRecord($"{taskPath} --run", null, null)
                ])
        ]));

        var evidence = provider.CollectEvidence(taskPath);

        var item = Assert.Single(evidence);
        Assert.Equal(@"\ExampleTask", item.Source);
    }

    [Fact]
    public void ShouldReturnTaskEvidenceWhenEnvironmentVariableCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_TASK_ROOT", sandbox.RootPath);
        var taskPath = sandbox.WriteFile("Tools\\task.exe");
        var provider = new ScheduledTaskEvidenceProvider(new StubWindowsScheduledTaskSource(
        [
            new WindowsScheduledTaskRecord(
                Path: @"\ExampleTask",
                Uri: null,
                Actions:
                [
                    new WindowsScheduledTaskActionRecord(@"%WINSAFECLEAN_TASK_ROOT%\Tools\task.exe", null, null)
                ])
        ]));

        var evidence = provider.CollectEvidence(taskPath);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenCommandDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile("target.exe");
        var otherPath = sandbox.WriteFile("other.exe");
        var provider = new ScheduledTaskEvidenceProvider(new StubWindowsScheduledTaskSource(
        [
            new WindowsScheduledTaskRecord(
                Path: @"\OtherTask",
                Uri: null,
                Actions:
                [
                    new WindowsScheduledTaskActionRecord(otherPath, null, null)
                ])
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsScheduledTaskSource(IReadOnlyList<WindowsScheduledTaskRecord> tasks)
        : IWindowsScheduledTaskSource
    {
        public IReadOnlyList<WindowsScheduledTaskRecord> GetScheduledTasks()
        {
            return tasks;
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
