using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.FileInventory;

public static class FileSystemScanner
{
    public static IReadOnlyList<ScanReportItem> Scan(string path, FileSystemScanOptions options)
    {
        return Scan(path, options, SystemFileSystem.Instance);
    }

    public static IReadOnlyList<ScanReportItem> Scan(string path, FileSystemScanOptions options, IFileSystem fileSystem)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(fileSystem);

        if (options.MaxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(options), "MaxItems must be greater than zero.");
        }

        if (!TryGetFullPath(path, fileSystem, out var normalizedPath, out var pathError))
        {
            return [CreateUnknownItem(path, "Path syntax is invalid: " + pathError)];
        }

        path = normalizedPath;

        if (fileSystem.FileExists(path))
        {
            return [CreateFileItem(path, fileSystem)];
        }

        if (fileSystem.DirectoryExists(path))
        {
            return ScanDirectory(path, options, fileSystem);
        }

        return [CreateMissingPathItem(path)];
    }

    private static IReadOnlyList<ScanReportItem> ScanDirectory(string path, FileSystemScanOptions options, IFileSystem fileSystem)
    {
        try
        {
            var entries = new List<string>(capacity: options.MaxItems);

            foreach (var entry in fileSystem.EnumerateFileSystemEntries(path))
            {
                entries.Add(entry);
                if (entries.Count >= options.MaxItems)
                {
                    break;
                }
            }

            return entries
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
                .Select(entry => CreateItem(entry, fileSystem))
                .ToList();
        }
        catch (UnauthorizedAccessException)
        {
            return [CreateUnknownItem(path, "Directory access was denied.", ScanReportItemKind.Directory)];
        }
        catch (PathTooLongException)
        {
            return [CreateUnknownItem(path, "Directory path was too long to enumerate.", ScanReportItemKind.Directory)];
        }
        catch (IOException)
        {
            return [CreateUnknownItem(path, "Directory could not be enumerated.", ScanReportItemKind.Directory)];
        }
        catch (System.Security.SecurityException)
        {
            return [CreateUnknownItem(path, "Directory access was blocked by security policy.", ScanReportItemKind.Directory)];
        }
    }

    private static bool TryGetFullPath(string path, IFileSystem fileSystem, out string normalizedPath, out string error)
    {
        try
        {
            normalizedPath = fileSystem.GetFullPath(path);
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

    private static ScanReportItem CreateItem(string path, IFileSystem fileSystem)
    {
        if (fileSystem.FileExists(path))
        {
            return CreateFileItem(path, fileSystem);
        }

        if (fileSystem.DirectoryExists(path))
        {
            return CreateDirectoryItem(path, fileSystem);
        }

        return CreateUnknownItem(path, "Path disappeared during scan.");
    }

    private static ScanReportItem CreateDirectoryItem(string path, IFileSystem fileSystem)
    {
        return new ScanReportItem(
            Path: path,
            ItemKind: ScanReportItemKind.Directory,
            SizeBytes: 0,
            LastWriteTimeUtc: TryGetLastWriteTimeUtc(() => fileSystem.GetDirectoryLastWriteTimeUtc(path)),
            Risk: PathRiskClassifier.Assess(path));
    }

    private static ScanReportItem CreateFileItem(string path, IFileSystem fileSystem)
    {
        long sizeBytes;

        try
        {
            sizeBytes = fileSystem.GetFileLength(path);
        }
        catch (UnauthorizedAccessException)
        {
            return CreateUnknownItem(path, "File access was denied.", ScanReportItemKind.File);
        }
        catch (PathTooLongException)
        {
            return CreateUnknownItem(path, "File path was too long to inspect.", ScanReportItemKind.File);
        }
        catch (IOException)
        {
            return CreateUnknownItem(path, "File metadata could not be read.", ScanReportItemKind.File);
        }
        catch (System.Security.SecurityException)
        {
            return CreateUnknownItem(path, "File access was blocked by security policy.", ScanReportItemKind.File);
        }

        return new ScanReportItem(
            Path: path,
            ItemKind: ScanReportItemKind.File,
            SizeBytes: sizeBytes,
            LastWriteTimeUtc: TryGetLastWriteTimeUtc(() => fileSystem.GetFileLastWriteTimeUtc(path)),
            Risk: PathRiskClassifier.Assess(path));
    }

    private static ScanReportItem CreateUnknownItem(
        string path,
        string reason,
        ScanReportItemKind itemKind = ScanReportItemKind.Unknown)
    {
        return new ScanReportItem(
            Path: path,
            ItemKind: itemKind,
            SizeBytes: 0,
            LastWriteTimeUtc: null,
            Risk: RiskAssessment.Unknown(reason));
    }

    private static ScanReportItem CreateMissingPathItem(string path)
    {
        var pathRisk = PathRiskClassifier.Assess(path);
        if (pathRisk.Level == RiskLevel.Blocked)
        {
            return new ScanReportItem(
                Path: path,
                ItemKind: ScanReportItemKind.Unknown,
                SizeBytes: 0,
                LastWriteTimeUtc: null,
                Risk: pathRisk);
        }

        return CreateUnknownItem(path, "Path does not exist or is not accessible.");
    }

    private static DateTimeOffset? TryGetLastWriteTimeUtc(Func<DateTimeOffset> getLastWriteTimeUtc)
    {
        try
        {
            return getLastWriteTimeUtc();
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
        }
        catch (IOException)
        {
            return null;
        }
        catch (System.Security.SecurityException)
        {
            return null;
        }
    }
}
