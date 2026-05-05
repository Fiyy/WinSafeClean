using WinSafeClean.CleanerRules;

namespace WinSafeClean.CleanerRules.Tests;

public sealed class CleanerMlParserTests
{
    [Fact]
    public void ShouldParseWindowsCleanerMetadataOptionsAndCandidates()
    {
        const string xml = """
            <cleaner id="example_app" os="windows">
              <label>Example App</label>
              <description>Example cleaner</description>
              <running type="exe" same_user="true">example.exe</running>
              <option id="cache">
                <label>Cache</label>
                <description>Delete cache candidates</description>
                <action command="delete" search="file" path="$localappdata\Example\cache.db"/>
                <action command="delete" search="glob" path="$localappdata\Example\Cache\*"/>
                <action command="delete" search="walk.files" path="$localappdata\Example\Cache" regex=".*\.tmp$"/>
              </option>
            </cleaner>
            """;

        var ruleSet = CleanerMlParser.Parse(xml);

        var cleaner = Assert.Single(ruleSet.Cleaners);
        Assert.Equal("example_app", cleaner.Id);
        Assert.Equal("Example App", cleaner.Label);
        Assert.Equal("Example cleaner", cleaner.Description);
        var blocker = Assert.Single(cleaner.RunningBlockers);
        Assert.Equal("exe", blocker.Type);
        Assert.Equal("example.exe", blocker.Value);
        Assert.True(blocker.SameUser);

        var option = Assert.Single(cleaner.Options);
        Assert.Equal("cache", option.Id);
        Assert.Equal("Cache", option.Label);
        Assert.Collection(
            option.Candidates,
            first =>
            {
                Assert.Equal(CleanerCandidateKind.File, first.Kind);
                Assert.Equal(@"$localappdata\Example\cache.db", first.PathPattern);
            },
            second =>
            {
                Assert.Equal(CleanerCandidateKind.Glob, second.Kind);
                Assert.Equal(@"$localappdata\Example\Cache\*", second.PathPattern);
            },
            third =>
            {
                Assert.Equal(CleanerCandidateKind.WalkFiles, third.Kind);
                Assert.Equal(@"$localappdata\Example\Cache", third.PathPattern);
                Assert.Equal(@".*\.tmp$", third.Regex);
            });
    }

    [Fact]
    public void ShouldIgnoreUnsupportedCommandsSearchesAndNonWindowsActions()
    {
        const string xml = """
            <cleaner id="example_app">
              <option id="cache">
                <action command="winreg" path="HKCU\Software\Example"/>
                <action command="process" path="example.exe"/>
                <action command="truncate" search="file" path="$localappdata\Example\log.txt"/>
                <action command="delete" search="deep" path="$USERPROFILE"/>
                <action command="delete" os="linux" search="file" path="~/.cache/example"/>
                <action command="delete" os="windows" search="walk.top" path="$localappdata\Example\Cache"/>
              </option>
            </cleaner>
            """;

        var ruleSet = CleanerMlParser.Parse(xml);

        var candidate = Assert.Single(Assert.Single(Assert.Single(ruleSet.Cleaners).Options).Candidates);
        Assert.Equal(CleanerCandidateKind.WalkTop, candidate.Kind);
        Assert.Equal(@"$localappdata\Example\Cache", candidate.PathPattern);
    }

    [Fact]
    public void ShouldSkipCleanerWhenOperatingSystemDoesNotMatch()
    {
        const string xml = """
            <cleaners>
              <cleaner id="linux_only" os="linux">
                <option id="cache">
                  <action command="delete" search="file" path="~/.cache/example"/>
                </option>
              </cleaner>
              <cleaner id="windows_app" os="windows">
                <option id="cache">
                  <action command="delete" search="file" path="$localappdata\Example\cache.db"/>
                </option>
              </cleaner>
            </cleaners>
            """;

        var ruleSet = CleanerMlParser.Parse(xml);

        var cleaner = Assert.Single(ruleSet.Cleaners);
        Assert.Equal("windows_app", cleaner.Id);
    }
}
