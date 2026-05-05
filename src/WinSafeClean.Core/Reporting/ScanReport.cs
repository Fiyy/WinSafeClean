namespace WinSafeClean.Core.Reporting;

public sealed record ScanReport(
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ScanReportItem> Items);
