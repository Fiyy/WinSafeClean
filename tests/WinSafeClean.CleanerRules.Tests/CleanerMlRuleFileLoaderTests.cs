namespace WinSafeClean.CleanerRules.Tests;

public sealed class CleanerMlRuleFileLoaderTests
{
    [Fact]
    public void ShouldLoadCleanerMlFile()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteText("example.xml", """
            <cleaner id="example">
              <option id="cache">
                <action command="delete" search="file" path="C:\Temp\cache.tmp"/>
              </option>
            </cleaner>
            """);

        var ruleSet = CleanerMlRuleFileLoader.LoadFile(filePath);

        var cleaner = Assert.Single(ruleSet.Cleaners);
        Assert.Equal("example", cleaner.Id);
    }

    [Fact]
    public void ShouldLoadCleanerMlFilesFromDirectoryInDeterministicOrder()
    {
        using var sandbox = TemporarySandbox.Create();
        sandbox.WriteText("b.xml", """
            <cleaner id="b">
              <option id="cache">
                <action command="delete" search="file" path="C:\Temp\b.tmp"/>
              </option>
            </cleaner>
            """);
        sandbox.WriteText("a.xml", """
            <cleaner id="a">
              <option id="cache">
                <action command="delete" search="file" path="C:\Temp\a.tmp"/>
              </option>
            </cleaner>
            """);
        sandbox.WriteText("ignored.txt", "<cleaner id=\"ignored\"/>");

        var ruleSet = CleanerMlRuleFileLoader.LoadDirectory(sandbox.RootPath);

        Assert.Collection(
            ruleSet.Cleaners,
            first => Assert.Equal("a", first.Id),
            second => Assert.Equal("b", second.Id));
    }

    [Fact]
    public void ShouldThrowWhenRuleFileIsMissing()
    {
        using var sandbox = TemporarySandbox.Create();
        var missing = Path.Combine(sandbox.RootPath, "missing.xml");

        Assert.Throws<FileNotFoundException>(() => CleanerMlRuleFileLoader.LoadFile(missing));
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
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.CleanerRules.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string WriteText(string relativePath, string contents)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
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
