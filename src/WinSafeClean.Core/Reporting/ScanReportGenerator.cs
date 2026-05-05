using WinSafeClean.Core.FileInventory;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportGenerator
{
    private const string CurrentSchemaVersion = "1.1";

    public static ScanReport Generate(string path, FileSystemScanOptions options, DateTimeOffset createdAt)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        return new ScanReport(
            SchemaVersion: CurrentSchemaVersion,
            CreatedAt: createdAt,
            Items: FileSystemScanner.Scan(path, options));
    }
}
