using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class ServiceEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnServiceEvidenceWhenQuotedImagePathMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var servicePath = sandbox.WriteFile("service.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("ExampleService", "Example Service", $"\"{servicePath}\" --run")
        ]));

        var evidence = provider.CollectEvidence(servicePath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.ServiceReference, item.Type);
        Assert.Equal("ExampleService (Example Service)", item.Source);
        Assert.Equal(0.95, item.Confidence);
        Assert.Contains("ImagePath", item.Message);
        Assert.Contains(servicePath, item.Message);
    }

    [Fact]
    public void ShouldReturnServiceEvidenceWhenUnquotedImagePathWithArgumentsMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var servicePath = sandbox.WriteFile("service.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("ExampleService", null, $"{servicePath} --run")
        ]));

        var evidence = provider.CollectEvidence(servicePath);

        var item = Assert.Single(evidence);
        Assert.Equal("ExampleService", item.Source);
    }

    [Fact]
    public void ShouldReturnServiceEvidenceWhenExpandedImagePathMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_TEST_ROOT", sandbox.RootPath);
        var servicePath = sandbox.WriteFile("System32\\svchost.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("SharedService", "Shared Service", @"%WINSAFECLEAN_TEST_ROOT%\System32\svchost.exe -k netsvcs")
        ]));

        var evidence = provider.CollectEvidence(servicePath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.ServiceReference, item.Type);
        Assert.Equal("SharedService (Shared Service)", item.Source);
    }

    [Fact]
    public void ShouldReturnServiceEvidenceWhenSystemRootImagePathMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("SystemRoot", sandbox.RootPath);
        var servicePath = sandbox.WriteFile("System32\\svchost.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("SharedService", "Shared Service", @"\SystemRoot\System32\svchost.exe -k netsvcs")
        ]));

        var evidence = provider.CollectEvidence(servicePath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.ServiceReference, item.Type);
        Assert.Equal("SharedService (Shared Service)", item.Source);
    }

    [Fact]
    public void ShouldReturnServiceEvidenceWhenUnquotedDirectoryContainsExecutableExtension()
    {
        using var sandbox = TemporarySandbox.Create();
        var servicePath = sandbox.WriteFile("Tools.exe\\service.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("ExampleService", null, $"{servicePath} --run")
        ]));

        var evidence = provider.CollectEvidence(servicePath);

        var item = Assert.Single(evidence);
        Assert.Equal("ExampleService", item.Source);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenImagePathDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile("target.exe");
        var otherPath = sandbox.WriteFile("other.exe");
        var provider = new ServiceEvidenceProvider(new StubWindowsServiceSource(
        [
            new WindowsServiceRecord("OtherService", null, otherPath)
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsServiceSource(IReadOnlyList<WindowsServiceRecord> services) : IWindowsServiceSource
    {
        public IReadOnlyList<WindowsServiceRecord> GetServices()
        {
            return services;
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
