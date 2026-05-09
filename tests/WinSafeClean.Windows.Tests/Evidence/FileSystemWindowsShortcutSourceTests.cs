using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class FileSystemWindowsShortcutSourceTests
{
    [Fact]
    public void ShouldReturnEmptyWhenShortcutRootDoesNotExist()
    {
        using var sandbox = TemporarySandbox.Create();
        var source = new FileSystemWindowsShortcutSource([Path.Combine(sandbox.RootPath, "missing")]);

        var shortcuts = source.GetShortcuts();

        Assert.Empty(shortcuts);
    }

    [Fact]
    public void ShouldSkipMalformedShortcutFiles()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteFile(@"Programs\Broken.lnk", "not a shortcut");
        var source = new FileSystemWindowsShortcutSource([sandbox.RootPath]);

        var shortcuts = source.GetShortcuts();

        Assert.Empty(shortcuts);
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.ShortcutSource.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public void WriteFile(string relativePath, string contents)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
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
