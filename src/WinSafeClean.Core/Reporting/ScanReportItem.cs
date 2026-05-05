using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Reporting;

public sealed record ScanReportItem(
    string Path,
    ScanReportItemKind ItemKind,
    long SizeBytes,
    DateTimeOffset? LastWriteTimeUtc,
    RiskAssessment Risk);
