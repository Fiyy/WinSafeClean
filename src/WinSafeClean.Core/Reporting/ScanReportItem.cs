using System.Text.Json.Serialization;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Reporting;

public sealed record ScanReportItem
{
    [JsonConstructor]
    public ScanReportItem(
        string Path,
        ScanReportItemKind ItemKind,
        long SizeBytes,
        DateTimeOffset? LastWriteTimeUtc,
        IReadOnlyList<EvidenceRecord> Evidence,
        RiskAssessment Risk)
    {
        this.Path = Path;
        this.ItemKind = ItemKind;
        this.SizeBytes = SizeBytes;
        this.LastWriteTimeUtc = LastWriteTimeUtc;
        this.Evidence = Evidence;
        this.Risk = Risk;
    }

    public ScanReportItem(
        string Path,
        ScanReportItemKind ItemKind,
        long SizeBytes,
        DateTimeOffset? LastWriteTimeUtc,
        RiskAssessment Risk)
        : this(Path, ItemKind, SizeBytes, LastWriteTimeUtc, [], Risk)
    {
    }

    public string Path { get; init; }

    public ScanReportItemKind ItemKind { get; init; }

    public long SizeBytes { get; init; }

    public DateTimeOffset? LastWriteTimeUtc { get; init; }

    public IReadOnlyList<EvidenceRecord> Evidence { get; init; }

    public RiskAssessment Risk { get; init; }
}
