using WinSafeClean.Core.Evidence;

namespace WinSafeClean.Windows.Evidence;

public static class WindowsEvidenceProviderFactory
{
    public static IReadOnlyList<IFileEvidenceProvider> CreateDefaultProviders()
    {
        return
        [
            new ServiceEvidenceProvider(),
            new ScheduledTaskEvidenceProvider(),
            new StartupEntryEvidenceProvider(),
            new UninstallRegistryEvidenceProvider(),
            new FileSignatureEvidenceProvider(),
            new RunningProcessEvidenceProvider()
        ];
    }
}
