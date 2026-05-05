using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class UninstallRegistryEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnUninstallEvidenceWhenUninstallStringMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var uninstallPath = sandbox.WriteFile("uninstall.exe");
        var provider = new UninstallRegistryEvidenceProvider(new StubWindowsUninstallEntrySource(
        [
            new WindowsUninstallEntryRecord(
                Scope: "HKLM",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                KeyName: "ExampleApp",
                DisplayName: "Example App",
                InstallLocation: null,
                UninstallString: $"\"{uninstallPath}\" /uninstall",
                QuietUninstallString: null,
                DisplayIcon: null)
        ]));

        var evidence = provider.CollectEvidence(uninstallPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.UninstallRegistryReference, item.Type);
        Assert.Equal(@"HKLM\Software\Microsoft\Windows\CurrentVersion\Uninstall\ExampleApp: Example App", item.Source);
        Assert.Equal(0.9, item.Confidence);
        Assert.Contains("UninstallString", item.Message);
        Assert.Contains(uninstallPath, item.Message);
    }

    [Fact]
    public void ShouldReturnUninstallEvidenceWhenDisplayIconWithIndexMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var appPath = sandbox.WriteFile("Example\\app.exe");
        var provider = new UninstallRegistryEvidenceProvider(new StubWindowsUninstallEntrySource(
        [
            new WindowsUninstallEntryRecord(
                Scope: "HKCU",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                KeyName: "ExampleApp",
                DisplayName: "Example App",
                InstallLocation: null,
                UninstallString: null,
                QuietUninstallString: null,
                DisplayIcon: $"{appPath},0")
        ]));

        var evidence = provider.CollectEvidence(appPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.UninstallRegistryReference, item.Type);
        Assert.Contains("DisplayIcon", item.Message);
    }

    [Fact]
    public void ShouldReturnInstalledApplicationEvidenceWhenPathIsInsideInstallLocation()
    {
        using var sandbox = TemporarySandbox.Create();
        var appPath = sandbox.WriteFile("Example\\bin\\app.exe");
        var provider = new UninstallRegistryEvidenceProvider(new StubWindowsUninstallEntrySource(
        [
            new WindowsUninstallEntryRecord(
                Scope: "HKLM",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                KeyName: "ExampleApp",
                DisplayName: "Example App",
                InstallLocation: Path.Combine(sandbox.RootPath, "Example"),
                UninstallString: null,
                QuietUninstallString: null,
                DisplayIcon: null)
        ]));

        var evidence = provider.CollectEvidence(appPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.InstalledApplication, item.Type);
        Assert.Equal(0.8, item.Confidence);
        Assert.Contains("InstallLocation", item.Message);
    }

    [Fact]
    public void ShouldReturnUninstallEvidenceWhenEnvironmentVariableCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_UNINSTALL_ROOT", sandbox.RootPath);
        var uninstallPath = sandbox.WriteFile("Example\\uninstall.exe");
        var provider = new UninstallRegistryEvidenceProvider(new StubWindowsUninstallEntrySource(
        [
            new WindowsUninstallEntryRecord(
                Scope: "HKCU",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                KeyName: "ExampleApp",
                DisplayName: null,
                InstallLocation: null,
                UninstallString: @"%WINSAFECLEAN_UNINSTALL_ROOT%\Example\uninstall.exe /remove",
                QuietUninstallString: null,
                DisplayIcon: null)
        ]));

        var evidence = provider.CollectEvidence(uninstallPath);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenNoUninstallEntryMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile("target.exe");
        var otherPath = sandbox.WriteFile("other.exe");
        var provider = new UninstallRegistryEvidenceProvider(new StubWindowsUninstallEntrySource(
        [
            new WindowsUninstallEntryRecord(
                Scope: "HKLM",
                Location: @"Software\Microsoft\Windows\CurrentVersion\Uninstall",
                KeyName: "OtherApp",
                DisplayName: "Other App",
                InstallLocation: Path.Combine(sandbox.RootPath, "Other"),
                UninstallString: otherPath,
                QuietUninstallString: null,
                DisplayIcon: null)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsUninstallEntrySource(IReadOnlyList<WindowsUninstallEntryRecord> entries)
        : IWindowsUninstallEntrySource
    {
        public IReadOnlyList<WindowsUninstallEntryRecord> GetUninstallEntries()
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
