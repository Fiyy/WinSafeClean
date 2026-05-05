using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.FileInventory;

namespace WinSafeClean.Core.Reporting;

public static class ScanReportGenerator
{
    private const string CurrentSchemaVersion = "1.3";

    public static ScanReport Generate(
        string path,
        FileSystemScanOptions options,
        DateTimeOffset createdAt,
        IFileEvidenceProvider? evidenceProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        var items = FileSystemScanner.Scan(path, options);
        if (evidenceProvider is not null)
        {
            items = items.Select(item => AttachEvidence(item, evidenceProvider)).ToArray();
        }

        return new ScanReport(
            SchemaVersion: CurrentSchemaVersion,
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: createdAt,
            Items: items);
    }

    private static ScanReportItem AttachEvidence(ScanReportItem item, IFileEvidenceProvider evidenceProvider)
    {
        try
        {
            return item with
            {
                Evidence = evidenceProvider.CollectEvidence(item.Path)
            };
        }
        catch (Exception ex)
        {
            return item with
            {
                Evidence =
                [
                    new EvidenceRecord(
                        Type: EvidenceType.CollectionFailure,
                        Source: evidenceProvider.GetType().Name,
                        Confidence: 1.0,
                        Message: $"Evidence provider failed with {ex.GetType().Name}.")
                ]
            };
        }
    }
}
