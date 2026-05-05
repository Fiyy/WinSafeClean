using WinSafeClean.Core.FileInventory;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportGenerator
{
    private const string CurrentSchemaVersion = "1.3";

    public static ScanReport Generate(string path, FileSystemScanOptions options, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        return new ScanReport(
            SchemaVersion: CurrentSchemaVersion,
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: createdAt,
            Items: FileSystemScanner.Scan(path, options));
    }
}
