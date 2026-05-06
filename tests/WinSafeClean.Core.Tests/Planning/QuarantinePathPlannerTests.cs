using WinSafeClean.Core.Planning;

namespace WinSafeClean.Core.Tests.Planning;

public sealed class QuarantinePathPlannerTests
{
    [Fact]
    public void ShouldGenerateDeterministicPreviewUnderConfiguredRoot()
    {
        const string sourcePath = @"C:\Users\Alice\AppData\Local\Example\cache.tmp";
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";

        var preview = QuarantinePathPlanner.CreatePreview(sourcePath, quarantineRoot);

        Assert.Equal(sourcePath, preview.OriginalPath);
        Assert.StartsWith(quarantineRoot, preview.ProposedQuarantinePath, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(quarantineRoot, preview.RestoreMetadataPath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(preview.RestorePlanId, preview.ProposedQuarantinePath);
        Assert.Contains(preview.RestorePlanId, preview.RestoreMetadataPath);
        Assert.EndsWith("-cache.tmp", preview.ProposedQuarantinePath, StringComparison.OrdinalIgnoreCase);
        Assert.EndsWith(".restore.json", preview.RestoreMetadataPath, StringComparison.OrdinalIgnoreCase);
        Assert.True(preview.RequiresManualConfirmation);
    }

    [Fact]
    public void ShouldSanitizeOriginalLeafNameForQuarantinePath()
    {
        const string sourcePath = @"C:\Temp\bad:name|part?.tmp";
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";

        var preview = QuarantinePathPlanner.CreatePreview(sourcePath, quarantineRoot);

        Assert.Contains("-bad_name_part_.tmp", preview.ProposedQuarantinePath);
        Assert.DoesNotContain("bad:name|part?", preview.ProposedQuarantinePath);
    }

    [Fact]
    public void ShouldRejectProtectedQuarantineRoot()
    {
        Assert.Throws<ArgumentException>(() =>
            QuarantinePathPlanner.CreatePreview(@"C:\Temp\cache.tmp", @"C:\Windows\Installer"));
    }

    [Fact]
    public void ShouldNotCreateQuarantineRoot()
    {
        var quarantineRoot = Path.Combine(Path.GetTempPath(), "WinSafeClean.Core.Tests", Path.GetRandomFileName());
        Assert.False(Directory.Exists(quarantineRoot));

        QuarantinePathPlanner.CreatePreview(@"C:\Temp\cache.tmp", quarantineRoot);

        Assert.False(Directory.Exists(quarantineRoot));
    }
}
