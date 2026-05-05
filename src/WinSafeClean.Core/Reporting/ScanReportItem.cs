using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Reporting;

public sealed record ScanReportItem(
    string Path,
    long SizeBytes,
    RiskAssessment Risk);
