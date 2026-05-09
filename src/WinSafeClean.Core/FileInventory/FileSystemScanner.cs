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

        options.CancellationToken.ThrowIfCancellationRequested();

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
        return options.Recursive
            ? ScanDirectoryRecursive(path, options, fileSystem)
            : ScanDirectoryDirectChildren(path, options, fileSystem);
    }

    private static IReadOnlyList<ScanReportItem> ScanDirectoryDirectChildren(
        string path,
        FileSystemScanOptions options,
        IFileSystem fileSystem)
    {
        try
        {
            var entries = new List<string>(capacity: options.MaxItems);

            foreach (var entry in fileSystem.EnumerateFileSystemEntries(path))
            {
                options.CancellationToken.ThrowIfCancellationRequested();

                entries.Add(entry);
                if (entries.Count >= options.MaxItems)
                {
                    break;
                }
            }

            return entries
                .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
                .Select(entry => CreateItem(entry, fileSystem, options))
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

    private static IReadOnlyList<ScanReportItem> ScanDirectoryRecursive(
        string path,
        FileSystemScanOptions options,
        IFileSystem fileSystem)
    {
        var items = new List<ScanReportItem>(capacity: options.MaxItems);
        AddRecursiveDirectoryChildren(
            path,
            options.MaxItems,
            fileSystem,
            items,
            options.CancellationToken,
            options.IncludeDirectorySizes,
            addEnumerationFailureItem: true);
        return items;
    }

    private static void AddRecursiveDirectoryChildren(
        string path,
        int maxItems,
        IFileSystem fileSystem,
        List<ScanReportItem> items,
        CancellationToken cancellationToken,
        bool includeDirectorySizes,
        bool addEnumerationFailureItem)
    {
        IReadOnlyList<string> entries;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            entries = EnumerateLimitedEntries(path, maxItems - items.Count, fileSystem, cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            AddEnumerationFailureItemIfNeeded(items, path, "Directory access was denied.", maxItems, addEnumerationFailureItem);
            return;
        }
        catch (PathTooLongException)
        {
            AddEnumerationFailureItemIfNeeded(items, path, "Directory path was too long to enumerate.", maxItems, addEnumerationFailureItem);
            return;
        }
        catch (IOException)
        {
            AddEnumerationFailureItemIfNeeded(items, path, "Directory could not be enumerated.", maxItems, addEnumerationFailureItem);
            return;
        }
        catch (System.Security.SecurityException)
        {
            AddEnumerationFailureItemIfNeeded(items, path, "Directory access was blocked by security policy.", maxItems, addEnumerationFailureItem);
            return;
        }

        foreach (var entry in entries)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (items.Count >= maxItems)
            {
                return;
            }

            var item = CreateItem(
                entry,
                fileSystem,
                new FileSystemScanOptions(
                    maxItems,
                    Recursive: true,
                    CancellationToken: cancellationToken,
                    IncludeDirectorySizes: includeDirectorySizes));
            items.Add(item);

            if (item.ItemKind != ScanReportItemKind.Directory
                || item.Risk.Level == RiskLevel.Blocked
                || TryIsReparsePoint(entry, fileSystem))
            {
                continue;
            }

            AddRecursiveDirectoryChildren(
                entry,
                maxItems,
                fileSystem,
                items,
                cancellationToken,
                includeDirectorySizes,
                addEnumerationFailureItem: false);
        }
    }

    private static void AddEnumerationFailureItemIfNeeded(
        List<ScanReportItem> items,
        string path,
        string reason,
        int maxItems,
        bool addEnumerationFailureItem)
    {
        if (addEnumerationFailureItem)
        {
            AddItemIfBudgetAllows(items, CreateUnknownItem(path, reason, ScanReportItemKind.Directory), maxItems);
        }
    }

    private static IReadOnlyList<string> EnumerateLimitedEntries(
        string path,
        int maxEntries,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var entries = new List<string>(capacity: Math.Max(0, maxEntries));

        if (maxEntries <= 0)
        {
            return entries;
        }

        foreach (var entry in fileSystem.EnumerateFileSystemEntries(path))
        {
            cancellationToken.ThrowIfCancellationRequested();

            entries.Add(entry);
            if (entries.Count >= maxEntries)
            {
                break;
            }
        }

        return entries
            .OrderBy(entry => entry, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static void AddItemIfBudgetAllows(List<ScanReportItem> items, ScanReportItem item, int maxItems)
    {
        if (items.Count < maxItems)
        {
            items.Add(item);
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

    private static ScanReportItem CreateItem(string path, IFileSystem fileSystem, FileSystemScanOptions options)
    {
        if (fileSystem.FileExists(path))
        {
            return CreateFileItem(path, fileSystem);
        }

        if (fileSystem.DirectoryExists(path))
        {
            return CreateDirectoryItem(path, fileSystem, options);
        }

        return CreateUnknownItem(path, "Path disappeared during scan.");
    }

    private static ScanReportItem CreateDirectoryItem(string path, IFileSystem fileSystem, FileSystemScanOptions options)
    {
        var risk = PathRiskClassifier.Assess(path);
        var sizeBytes = 0L;

        if (options.IncludeDirectorySizes
            && risk.Level != RiskLevel.Blocked
            && !TryIsReparsePoint(path, fileSystem))
        {
            var directorySize = CalculateDirectorySize(path, fileSystem, options.CancellationToken);
            sizeBytes = directorySize.SizeBytes;
            if (!string.IsNullOrWhiteSpace(directorySize.Warning))
            {
                risk = AddReason(risk, directorySize.Warning);
            }
        }

        return new ScanReportItem(
            Path: path,
            ItemKind: ScanReportItemKind.Directory,
            SizeBytes: sizeBytes,
            LastWriteTimeUtc: TryGetLastWriteTimeUtc(() => fileSystem.GetDirectoryLastWriteTimeUtc(path)),
            Risk: risk);
    }

    private static DirectorySizeResult CalculateDirectorySize(
        string path,
        IFileSystem fileSystem,
        CancellationToken cancellationToken)
    {
        var pending = new Stack<string>();
        pending.Push(path);
        long sizeBytes = 0;

        while (pending.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var directory = pending.Pop();

            IReadOnlyList<string> entries;
            try
            {
                entries = fileSystem.EnumerateFileSystemEntries(directory).ToList();
            }
            catch (UnauthorizedAccessException)
            {
                return new DirectorySizeResult(sizeBytes, "Directory size could not be fully calculated because access was denied.");
            }
            catch (PathTooLongException)
            {
                return new DirectorySizeResult(sizeBytes, "Directory size could not be fully calculated because a path was too long.");
            }
            catch (IOException)
            {
                return new DirectorySizeResult(sizeBytes, "Directory size could not be fully calculated because a directory could not be enumerated.");
            }
            catch (System.Security.SecurityException)
            {
                return new DirectorySizeResult(sizeBytes, "Directory size could not be fully calculated because access was blocked by security policy.");
            }

            foreach (var entry in entries)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (fileSystem.FileExists(entry))
                {
                    var fileSize = TryGetFileLengthForDirectorySize(entry, fileSystem, out var warning);
                    sizeBytes += fileSize;
                    if (!string.IsNullOrWhiteSpace(warning))
                    {
                        return new DirectorySizeResult(sizeBytes, warning);
                    }

                    continue;
                }

                if (!fileSystem.DirectoryExists(entry)
                    || TryIsReparsePoint(entry, fileSystem)
                    || PathRiskClassifier.Assess(entry).Level == RiskLevel.Blocked)
                {
                    continue;
                }

                pending.Push(entry);
            }
        }

        return new DirectorySizeResult(sizeBytes, Warning: null);
    }

    private static long TryGetFileLengthForDirectorySize(string path, IFileSystem fileSystem, out string? warning)
    {
        try
        {
            warning = null;
            return fileSystem.GetFileLength(path);
        }
        catch (UnauthorizedAccessException)
        {
            warning = "Directory size could not be fully calculated because file access was denied.";
            return 0;
        }
        catch (PathTooLongException)
        {
            warning = "Directory size could not be fully calculated because a file path was too long.";
            return 0;
        }
        catch (IOException)
        {
            warning = "Directory size could not be fully calculated because file metadata could not be read.";
            return 0;
        }
        catch (System.Security.SecurityException)
        {
            warning = "Directory size could not be fully calculated because file access was blocked by security policy.";
            return 0;
        }
    }

    private static RiskAssessment AddReason(RiskAssessment risk, string reason)
    {
        return risk with
        {
            Reasons = risk.Reasons.Concat([reason]).ToArray(),
            Confidence = Math.Min(risk.Confidence, 0.2)
        };
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

    private static bool TryIsReparsePoint(string path, IFileSystem fileSystem)
    {
        try
        {
            return fileSystem.IsReparsePoint(path);
        }
        catch (UnauthorizedAccessException)
        {
            return true;
        }
        catch (PathTooLongException)
        {
            return true;
        }
        catch (IOException)
        {
            return true;
        }
        catch (System.Security.SecurityException)
        {
            return true;
        }
    }

    private sealed record DirectorySizeResult(long SizeBytes, string? Warning);
}
