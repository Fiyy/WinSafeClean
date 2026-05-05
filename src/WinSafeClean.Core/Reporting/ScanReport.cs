namespace WinSafeClean.Core.Reporting;

public sealed record ScanReport(
    string SchemaVersion,
    ScanReportPrivacyMode PrivacyMode,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ScanReportItem> Items)
{
    public ScanReport(
        string SchemaVersion,
        DateTimeOffset CreatedAt,
        IReadOnlyList<ScanReportItem> Items)
        : this(SchemaVersion, ScanReportPrivacyMode.Full, CreatedAt, Items)
    {
    }
}
