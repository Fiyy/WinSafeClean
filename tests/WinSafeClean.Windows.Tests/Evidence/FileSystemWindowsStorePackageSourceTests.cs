using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class FileSystemWindowsStorePackageSourceTests
{
    [Fact]
    public void ShouldReturnEmptyWhenPackageRootDoesNotExist()
    {
        using var sandbox = TemporarySandbox.Create();
        var source = new FileSystemWindowsStorePackageSource(
        [
            new WindowsStorePackageRoot(Path.Combine(sandbox.RootPath, "missing"), "Package data root")
        ]);

        var packages = source.GetStorePackages();

        Assert.Empty(packages);
    }

    [Fact]
    public void ShouldReadTopLevelPackageDirectories()
    {
        using var sandbox = TemporarySandbox.Create();
        var firstPackage = sandbox.CreateDirectory(@"Packages\Contoso.App_abc123");
        var secondPackage = sandbox.CreateDirectory(@"Packages\Fabrikam.Tool_xyz456");
        sandbox.CreateDirectory(@"Packages\Contoso.App_abc123\Nested");
        var source = new FileSystemWindowsStorePackageSource(
        [
            new WindowsStorePackageRoot(Path.Combine(sandbox.RootPath, "Packages"), "Package data root")
        ]);

        var packages = source.GetStorePackages();

        Assert.Collection(
            packages.OrderBy(package => package.PackageName, StringComparer.OrdinalIgnoreCase),
            package =>
            {
                Assert.Equal("Contoso.App_abc123", package.PackageName);
                Assert.Equal(firstPackage, package.PackageRoot);
                Assert.Equal("Package data root", package.RootKind);
            },
            package =>
            {
                Assert.Equal("Fabrikam.Tool_xyz456", package.PackageName);
                Assert.Equal(secondPackage, package.PackageRoot);
            });
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.StorePackageSource.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
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
