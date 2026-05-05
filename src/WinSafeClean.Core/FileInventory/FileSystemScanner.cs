using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.FileInventory;

public static class FileSystemScanner
{
    public static IReadOnlyList<ScanReportItem> Scan(string path, FileSystemScanOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);

        if (options.MaxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxItems must be greater than zero.");
        }

        if (!TryGetFullPath(path, out var normalizedPath, out var pathError))
        {
            return [CreateUnknownItem(path, "Path syntax is invalid: " + pathError)];
        }

        path = normalizedPath;

        if (File.Exists(path))
        {
            return [CreateFileItem(path)];
        }

        if (Directory.Exists(path))
        {
            return ScanDirectory(path, options);
        }

        return [CreateMissingPathItem(path)];
    }

    private static IReadOnlyList<ScanReportItem> ScanDirectory(string path, FileSystemScanOptions options)
    {
        try
        {
            var entries = new List<string>(capacity: options.MaxItems);

            foreach (var entry in Directory.EnumerateFileSystemEntries(path))
            {
                entries.Add(entry);
                if (entries.Count >= options.MaxItems)
                {
                    break;
                }
            }

            return entries
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
                .Select(CreateItem)
                .ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return [CreateUnknownItem(path, "Directory access was denied.")];
        }
        catch (PathTooLongException)
        {
            return [CreateUnknownItem(path, "Directory path was too long to enumerate.")];
        }
        catch (IOException)
        {
            return [CreateUnknownItem(path, "Directory could not be enumerated.")];
        }
        catch (System.Security.SecurityException)
        {
            return [CreateUnknownItem(path, "Directory access was blocked by security policy.")];
        }
    }

    private static bool TryGetFullPath(string path, out string normalizedPath, out string error)
    {
        try
        {
            normalizedPath = Path.GetFullPath(path);
            error = string.Empty;
            return true;
        }
        catch (ArgumentException ex)
        {
            normalizedPath = path;
            error = ex.Message;
            return false;
        }
        catch (NotSupportedException ex)
        {
            normalizedPath = path;
            error = ex.Message;
            return false;
        }
        catch (PathTooLongException ex)
        {
            normalizedPath = path;
            error = ex.Message;
            return false;
        }
        catch (System.Security.SecurityException ex)
        {
            normalizedPath = path;
            error = ex.Message;
            return false;
        }
    }

    private static ScanReportItem CreateItem(string path)
    {
        if (File.Exists(path))
        {
            return CreateFileItem(path);
        }

        if (Directory.Exists(path))
        {
            return new ScanReportItem(
                Path: path,
                SizeBytes: 0,
                Risk: PathRiskClassifier.Assess(path));
        }

        return CreateUnknownItem(path, "Path disappeared during scan.");
    }

    private static ScanReportItem CreateFileItem(string path)
    {
        long sizeBytes;

        try
        {
            sizeBytes = new FileInfo(path).Length;
        }
        catch (UnauthorizedAccessException)
        {
            return CreateUnknownItem(path, "File access was denied.");
        }
        catch (PathTooLongException)
        {
            return CreateUnknownItem(path, "File path was too long to inspect.");
        }
        catch (IOException)
        {
            return CreateUnknownItem(path, "File metadata could not be read.");
        }
        catch (System.Security.SecurityException)
        {
            return CreateUnknownItem(path, "File access was blocked by security policy.");
        }

        return new ScanReportItem(
            Path: path,
            SizeBytes: sizeBytes,
            Risk: PathRiskClassifier.Assess(path));
    }

    private static ScanReportItem CreateUnknownItem(string path, string reason)
    {
        return new ScanReportItem(
            Path: path,
            SizeBytes: 0,
            Risk: RiskAssessment.Unknown(reason));
    }

    private static ScanReportItem CreateMissingPathItem(string path)
    {
        var pathRisk = PathRiskClassifier.Assess(path);
        if (pathRisk.Level == RiskLevel.Blocked)
        {
            return new ScanReportItem(
                Path: path,
                SizeBytes: 0,
                Risk: pathRisk);
        }

        return CreateUnknownItem(path, "Path does not exist or is not accessible.");
    }
}
