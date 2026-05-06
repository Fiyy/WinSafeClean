using WinSafeClean.Core.Evidence;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class WindowsEvidenceProviderFactoryTests
{
    [Fact]
    public void ShouldCreateDefaultReadOnlyEvidenceProviders()
    {
        var providers = WindowsEvidenceProviderFactory.CreateDefaultProviders();

        Assert.Collection(
            providers,
            provider => Assert.IsType<ServiceEvidenceProvider>(provider),
            provider => Assert.IsType<ScheduledTaskEvidenceProvider>(provider),
            provider => Assert.IsType<StartupEntryEvidenceProvider>(provider),
            provider => Assert.IsType<UninstallRegistryEvidenceProvider>(provider),
            provider => Assert.IsType<FileSignatureEvidenceProvider>(provider),
            provider => Assert.IsType<RunningProcessEvidenceProvider>(provider));
    }

    [Fact]
    public void DefaultProvidersShouldReturnEmptyEvidenceForMissingPath()
    {
        var composite = new CompositeFileEvidenceProvider(WindowsEvidenceProviderFactory.CreateDefaultProviders());

        var evidence = composite.CollectEvidence(@"C:\Tools\app.exe");

        Assert.Empty(evidence);
    }
}
