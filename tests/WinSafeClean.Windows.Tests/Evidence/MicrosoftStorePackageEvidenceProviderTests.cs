using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class MicrosoftStorePackageEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnStorePackageEvidenceWhenPathIsInsidePackageRoot()
    {
        using var sandbox = TemporarySandbox.Create();
        var packageRoot = sandbox.CreateDirectory("Contoso.App_1.0.0.0_x64__abc123");
        var targetPath = sandbox.WriteFile(@"Contoso.App_1.0.0.0_x64__abc123\Assets\logo.png");
        var provider = new MicrosoftStorePackageEvidenceProvider(new StubWindowsStorePackageSource(
        [
            new WindowsStorePackageRecord("Contoso.App_1.0.0.0_x64__abc123", packageRoot, "Package install location")
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.MicrosoftStorePackage, item.Type);
        Assert.Equal("Microsoft Store package: Contoso.App_1.0.0.0_x64__abc123", item.Source);
        Assert.Equal(0.85, item.Confidence);
        Assert.Contains("Microsoft Store package", item.Message);
        Assert.Contains(packageRoot, item.Message);
    }

    [Fact]
    public void ShouldReturnStorePackageEvidenceWhenPathMatchesPackageRoot()
    {
        using var sandbox = TemporarySandbox.Create();
        var packageRoot = sandbox.CreateDirectory("Contoso.App_abc123");
        var provider = new MicrosoftStorePackageEvidenceProvider(new StubWindowsStorePackageSource(
        [
            new WindowsStorePackageRecord("Contoso.App_abc123", packageRoot, "Package data root")
        ]));

        var evidence = provider.CollectEvidence(packageRoot);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenPathIsOutsidePackageRoot()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile(@"Other\file.txt");
        var packageRoot = sandbox.CreateDirectory("Contoso.App_abc123");
        var provider = new MicrosoftStorePackageEvidenceProvider(new StubWindowsStorePackageSource(
        [
            new WindowsStorePackageRecord("Contoso.App_abc123", packageRoot, "Package data root")
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsStorePackageSource(IReadOnlyList<WindowsStorePackageRecord> packages)
        : IWindowsStorePackageSource
    {
        public IReadOnlyList<WindowsStorePackageRecord> GetStorePackages()
        {
            return packages;
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.StorePackage.Tests", Path.GetRandomFileName());
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
}
