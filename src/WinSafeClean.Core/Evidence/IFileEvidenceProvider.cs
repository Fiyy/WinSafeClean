using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Core.Evidence;

public interface IFileEvidenceProvider
{
    IReadOnlyList<EvidenceRecord> CollectEvidence(string path);
}
