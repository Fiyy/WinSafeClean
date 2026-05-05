using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class StartupEntryEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnStartupEvidenceWhenQuotedCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var startupPath = sandbox.WriteFile("startup.exe");
        var provider = new StartupEntryEvidenceProvider(new StubWindowsStartupEntrySource(
        [
            new WindowsStartupEntryRecord(
                Scope: "HKCU",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Run",
                Name: "ExampleStartup",
                Command: $"\"{startupPath}\" --background")
        ]));

        var evidence = provider.CollectEvidence(startupPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.StartupReference, item.Type);
        Assert.Equal(@"HKCU\Software\Microsoft\Windows\CurrentVersion\Run: ExampleStartup", item.Source);
        Assert.Equal(0.85, item.Confidence);
        Assert.Contains("Startup entry", item.Message);
        Assert.Contains(startupPath, item.Message);
        Assert.Contains("--background", item.Message);
    }

    [Fact]
    public void ShouldReturnStartupEvidenceWhenUnquotedCommandWithArgumentsMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var startupPath = sandbox.WriteFile("startup.exe");
        var provider = new StartupEntryEvidenceProvider(new StubWindowsStartupEntrySource(
        [
            new WindowsStartupEntryRecord(
                Scope: "HKLM",
                Location: @"Software\Microsoft\Windows\CurrentVersion\RunOnce",
                Name: "ExampleStartup",
                Command: $"{startupPath} --once")
        ]));

        var evidence = provider.CollectEvidence(startupPath);

        var item = Assert.Single(evidence);
        Assert.Equal(@"HKLM\Software\Microsoft\Windows\CurrentVersion\RunOnce: ExampleStartup", item.Source);
    }

    [Fact]
    public void ShouldReturnStartupEvidenceWhenEnvironmentVariableCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_STARTUP_ROOT", sandbox.RootPath);
        var startupPath = sandbox.WriteFile("Tools\\startup.exe");
        var provider = new StartupEntryEvidenceProvider(new StubWindowsStartupEntrySource(
        [
            new WindowsStartupEntryRecord(
                Scope: "HKCU",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Run",
                Name: "ExampleStartup",
                Command: @"%WINSAFECLEAN_STARTUP_ROOT%\Tools\startup.exe --background")
        ]));

        var evidence = provider.CollectEvidence(startupPath);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenCommandDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile("target.exe");
        var otherPath = sandbox.WriteFile("other.exe");
        var provider = new StartupEntryEvidenceProvider(new StubWindowsStartupEntrySource(
        [
            new WindowsStartupEntryRecord(
                Scope: "HKCU",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Run",
                Name: "OtherStartup",
                Command: otherPath)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsStartupEntrySource(IReadOnlyList<WindowsStartupEntryRecord> entries)
        : IWindowsStartupEntrySource
    {
        public IReadOnlyList<WindowsStartupEntryRecord> GetStartupEntries()
        {
            return entries;
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
