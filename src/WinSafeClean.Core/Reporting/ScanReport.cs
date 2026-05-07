using System.Text.Json.Serialization;

namespace WinSafeClean.Core.Reporting;

public sealed record ScanReport
{
    [JsonConstructor]
    public ScanReport(
        string SchemaVersion,
        ScanReportPrivacyMode PrivacyMode,
        DateTimeOffset CreatedAt,
        IReadOnlyList<ScanReportItem> Items)
    {
        this.SchemaVersion = SchemaVersion;
        this.PrivacyMode = PrivacyMode;
        this.CreatedAt = CreatedAt;
        this.Items = Items;
    }

    public ScanReport(
        string SchemaVersion,
        DateTimeOffset CreatedAt,
        IReadOnlyList<ScanReportItem> Items)
        : this(SchemaVersion, ScanReportPrivacyMode.Full, CreatedAt, Items)
    {
    }

    public string SchemaVersion { get; init; }

    public ScanReportPrivacyMode PrivacyMode { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    public IReadOnlyList<ScanReportItem> Items { get; init; }
}
