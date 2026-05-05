using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class RunningProcessEvidenceProvider : IFileEvidenceProvider
{
    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        return [];
    }
}
