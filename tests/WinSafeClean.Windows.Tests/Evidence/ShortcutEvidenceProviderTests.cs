using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class ShortcutEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnShortcutEvidenceWhenTargetMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile(@"Tools\example.exe");
        var shortcutPath = sandbox.WriteFile(@"Desktop\Example.lnk");
        var provider = new ShortcutEvidenceProvider(new StubWindowsShortcutSource(
        [
            new WindowsShortcutRecord(shortcutPath, targetPath, "--run", null)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.ShortcutReference, item.Type);
        Assert.Equal(shortcutPath, item.Source);
        Assert.Equal(0.8, item.Confidence);
        Assert.Contains("Windows shortcut", item.Message);
        Assert.Contains(targetPath, item.Message);
        Assert.Contains("--run", item.Message);
    }

    [Fact]
    public void ShouldExpandEnvironmentVariablesAndTrimQuotesInShortcutTarget()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_SHORTCUT_ROOT", sandbox.RootPath);
        var targetPath = sandbox.WriteFile(@"Tools\example.exe");
        var provider = new ShortcutEvidenceProvider(new StubWindowsShortcutSource(
        [
            new WindowsShortcutRecord(
                ShortcutPath: @"C:\Users\Alice\Desktop\Example.lnk",
                TargetPath: @"""%WINSAFECLEAN_SHORTCUT_ROOT%\Tools\example.exe""",
                Arguments: null,
                WorkingDirectory: null)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenShortcutTargetDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile(@"Tools\example.exe");
        var otherPath = sandbox.WriteFile(@"Tools\other.exe");
        var provider = new ShortcutEvidenceProvider(new StubWindowsShortcutSource(
        [
            new WindowsShortcutRecord(@"C:\Users\Alice\Desktop\Other.lnk", otherPath, null, null)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsShortcutSource(IReadOnlyList<WindowsShortcutRecord> records)
        : IWindowsShortcutSource
    {
        public IReadOnlyList<WindowsShortcutRecord> GetShortcuts()
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.Shortcut.Tests", Path.GetRandomFileName());
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
