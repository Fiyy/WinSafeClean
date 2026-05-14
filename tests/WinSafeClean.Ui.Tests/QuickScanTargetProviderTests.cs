using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class QuickScanTargetProviderTests
{
    [Fact]
    public void CreateDefault_ShouldSuggestCommonUserScanTargets()
    {
        var targets = QuickScanTargetProvider.CreateDefault(new QuickScanTargetPathSource(
            UserProfilePath: @"C:\Users\Alice",
            DesktopPath: @"C:\Users\Alice\Desktop",
            LocalAppDataPath: @"C:\Users\Alice\AppData\Local",
            TempPath: @"C:\Users\Alice\AppData\Local\Temp"));

        Assert.Equal(
            ["Downloads", "Desktop", "User Temp", "Local AppData"],
            targets.Select(target => target.DisplayName));
        Assert.Contains(targets, target => target.Path == @"C:\Users\Alice\Downloads");
        Assert.All(targets, target => Assert.Contains("read-only", target.Description, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateDefault_ShouldSkipEmptyDuplicateAndProtectedWindowsTargets()
    {
        var targets = QuickScanTargetProvider.CreateDefault(new QuickScanTargetPathSource(
            UserProfilePath: @"C:\Users\Alice",
            DesktopPath: @"C:\Users\Alice\Downloads",
            LocalAppDataPath: @"C:\Windows\System32",
            TempPath: ""));

        Assert.Equal(["Downloads"], targets.Select(target => target.DisplayName));
        Assert.DoesNotContain(targets, target => target.Path.Contains(@"Windows\System32", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void CreateDefault_ShouldNormalizeTrailingSeparatorsForDeduplication()
    {
        var targets = QuickScanTargetProvider.CreateDefault(new QuickScanTargetPathSource(
            UserProfilePath: @"C:\Users\Alice\",
            DesktopPath: @"C:\Users\Alice\Downloads\",
            LocalAppDataPath: @"C:\Users\Alice\AppData\Local\",
            TempPath: @"C:\Users\Alice\AppData\Local\Temp\"));

        Assert.Equal(3, targets.Count);
        Assert.Equal(@"C:\Users\Alice\Downloads", targets[0].Path);
    }
}
