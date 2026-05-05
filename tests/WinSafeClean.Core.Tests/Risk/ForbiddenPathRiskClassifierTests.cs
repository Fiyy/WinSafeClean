using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Risk;

public sealed class ForbiddenPathRiskClassifierTests
{
    [Theory]
    [InlineData(@"C:\Windows\Installer")]
    [InlineData(@"C:\Windows\Installer\abc.msi")]
    [InlineData(@"c:\windows\installer\abc.msp")]
    [InlineData(@"C:/Windows/Installer/abc.msi")]
    [InlineData(@"D:\Windows\Installer\abc.msi")]
    public void ShouldBlockWindowsInstallerCache(string path)
    {
        var assessment = PathRiskClassifier.Assess(path);

        Assert.Equal(RiskLevel.Blocked, assessment.Level);
        Assert.Equal(SuggestedAction.Keep, assessment.SuggestedAction);
        Assert.Contains(assessment.Blockers, blocker => blocker.Contains("Windows Installer cache", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(@"C:\Windows\WinSxS")]
    [InlineData(@"C:\Windows\WinSxS\amd64_microsoft-windows")]
    [InlineData(@"C:\Windows\System32")]
    [InlineData(@"C:\Windows\System32\drivers\etc\hosts")]
    [InlineData(@"C:\Windows\System32\DriverStore")]
    [InlineData(@"C:\Windows\System32\DriverStore\FileRepository")]
    [InlineData(@"C:\Windows\servicing")]
    [InlineData(@"C:\Windows\SysWOW64")]
    [InlineData(@"C:\Windows\SystemApps")]
    [InlineData(@"C:\Windows\INF")]
    [InlineData(@"D:\Windows\System32")]
    public void ShouldBlockSystemManagedWindowsDirectories(string path)
    {
        var assessment = PathRiskClassifier.Assess(path);

        Assert.Equal(RiskLevel.Blocked, assessment.Level);
        Assert.Equal(SuggestedAction.SuggestWindowsTool, assessment.SuggestedAction);
        Assert.NotEmpty(assessment.Blockers);
    }

    [Theory]
    [InlineData(@"C:\Windows\Temp\..\Installer\abc.msi")]
    [InlineData(@"C:\Windows\System32\..\Installer\abc.msi")]
    [InlineData(@"\\?\C:\Windows\Installer\abc.msi")]
    [InlineData(@"\\localhost\c$\Windows\Installer\abc.msi")]
    [InlineData(@"C:\Windows\Installer\")]
    [InlineData(@"  C:\Windows\Installer\abc.msi  ")]
    [InlineData(@"C:\Windows\\Installer\abc.msi")]
    public void ShouldBlockNormalizedWindowsInstallerVariants(string path)
    {
        var assessment = PathRiskClassifier.Assess(path);

        Assert.Equal(RiskLevel.Blocked, assessment.Level);
        Assert.Equal(SuggestedAction.Keep, assessment.SuggestedAction);
    }

    [Fact]
    public void ShouldDescribeDriverStoreProtection()
    {
        var assessment = PathRiskClassifier.Assess(@"C:\Windows\System32\DriverStore\FileRepository");

        Assert.Equal(RiskLevel.Blocked, assessment.Level);
        Assert.Contains(assessment.Blockers, blocker => blocker.Contains("DriverStore", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData(@"C:\Windows\InstallerBackup")]
    [InlineData(@"C:\Windows.old\Installer")]
    [InlineData(@"C:\Users\Alice\AppData\Local\Temp")]
    public void ShouldNotBlockPathsThatOnlyLookSimilar(string path)
    {
        var assessment = PathRiskClassifier.Assess(path);

        Assert.NotEqual(RiskLevel.Blocked, assessment.Level);
    }
}
