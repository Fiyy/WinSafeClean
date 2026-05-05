using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Reporting;

public sealed record ScanReportItem(
    string Path,
    ScanReportItemKind ItemKind,
    long SizeBytes,
    DateTimeOffset? LastWriteTimeUtc,
    IReadOnlyList<EvidenceRecord> Evidence,
    RiskAssessment Risk)
{
    public ScanReportItem(
        string Path,
        ScanReportItemKind ItemKind,
        long SizeBytes,
        DateTimeOffset? LastWriteTimeUtc,
        RiskAssessment Risk)
        : this(Path, ItemKind, SizeBytes, LastWriteTimeUtc, [], Risk)
    {
    }
}
