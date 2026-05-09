using System.Globalization;

namespace WinSafeClean.Ui.Operations;

public sealed record ReadOnlyScanCommandOptions(
    string Path,
    bool Recursive = false,
    int? MaxItems = null,
    string Format = "json",
    string Privacy = "full",
    string? OutputPath = null,
    string? CleanerMlPath = null,
    bool IncludeDirectorySizes = false);

public sealed record ReadOnlyPlanCommandOptions(
    string Path,
    bool Recursive = false,
    int? MaxItems = null,
    string? CleanerMlPath = null,
    string Format = "json",
    string Privacy = "full",
    string? OutputPath = null,
    bool IncludeDirectorySizes = false);

public sealed record ReadOnlyPreflightCommandOptions(
    string PlanPath,
    string MetadataPath,
    bool ManualConfirmation = false,
    string Format = "json",
    string? OutputPath = null);

public static class ReadOnlyOperationCommandBuilder
{
    public static int? ParseMaxItems(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var maxItems) || maxItems <= 0)
        {
            throw new ArgumentException("Max items must be a positive integer.");
        }

        return maxItems;
    }

    public static IReadOnlyList<string> BuildScan(
        string path,
        bool recursive = false,
        int? maxItems = null)
    {
        return BuildScan(new ReadOnlyScanCommandOptions(path, recursive, maxItems));
    }

    public static IReadOnlyList<string> BuildScan(ReadOnlyScanCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);

        var args = new List<string> { "scan", "--path", options.Path };
        AddSharedScanOptions(
            args,
            options.Recursive,
            options.MaxItems,
            options.CleanerMlPath,
            options.Format,
            options.Privacy,
            options.OutputPath,
            options.IncludeDirectorySizes);

        return args;
    }

    public static IReadOnlyList<string> BuildPlan(
        string path,
        string? cleanerMlPath = null)
    {
        return BuildPlan(new ReadOnlyPlanCommandOptions(path, CleanerMlPath: cleanerMlPath));
    }

    public static IReadOnlyList<string> BuildPlan(ReadOnlyPlanCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.Path);

        var args = new List<string> { "plan", "--path", options.Path };
        AddSharedScanOptions(
            args,
            options.Recursive,
            options.MaxItems,
            options.CleanerMlPath,
            options.Format,
            options.Privacy,
            options.OutputPath,
            options.IncludeDirectorySizes);

        return args;
    }

    public static IReadOnlyList<string> BuildPreflight(
        string planPath,
        string metadataPath,
        bool manualConfirmation = false)
    {
        return BuildPreflight(new ReadOnlyPreflightCommandOptions(planPath, metadataPath, manualConfirmation));
    }

    public static IReadOnlyList<string> BuildPreflight(ReadOnlyPreflightCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PlanPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.MetadataPath);

        var args = new List<string> { "preflight", "--plan", options.PlanPath, "--metadata", options.MetadataPath };
        if (options.ManualConfirmation)
        {
            args.Add("--manual-confirmation");
        }

        AddFormat(args, options.Format);
        AddOutput(args, options.OutputPath);

        return args;
    }

    private static void AddSharedScanOptions(
        List<string> args,
        bool recursive,
        int? maxItems,
        string? cleanerMlPath,
        string format,
        string privacy,
        string? outputPath,
        bool includeDirectorySizes)
    {
        if (recursive)
        {
            args.Add("--recursive");
        }

        if (maxItems is not null)
        {
            ValidateMaxItems(maxItems.Value);
            args.Add("--max-items");
            args.Add(maxItems.Value.ToString(CultureInfo.InvariantCulture));
        }

        if (!string.IsNullOrWhiteSpace(cleanerMlPath))
        {
            args.Add("--cleanerml");
            args.Add(cleanerMlPath);
        }

        if (includeDirectorySizes)
        {
            args.Add("--directory-sizes");
        }

        AddFormat(args, format);
        AddPrivacy(args, privacy);
        AddOutput(args, outputPath);
    }

    private static void AddFormat(List<string> args, string format)
    {
        var normalized = NormalizeFormat(format);
        if (!normalized.Equals("json", StringComparison.Ordinal))
        {
            args.Add("--format");
            args.Add(normalized);
        }
    }

    private static void AddPrivacy(List<string> args, string privacy)
    {
        var normalized = NormalizePrivacy(privacy);
        if (!normalized.Equals("full", StringComparison.Ordinal))
        {
            args.Add("--privacy");
            args.Add(normalized);
        }
    }

    private static void AddOutput(List<string> args, string? outputPath)
    {
        if (!string.IsNullOrWhiteSpace(outputPath))
        {
            args.Add("--output");
            args.Add(outputPath);
        }
    }

    private static void ValidateMaxItems(int maxItems)
    {
        if (maxItems <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxItems), "Max items must be positive.");
        }
    }

    private static string NormalizeFormat(string? format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return "json";
        }

        var normalized = format.Trim().ToLowerInvariant();
        return normalized is "json" or "markdown"
            ? normalized
            : throw new ArgumentException("Format must be either 'json' or 'markdown'.");
    }

    private static string NormalizePrivacy(string? privacy)
    {
        if (string.IsNullOrWhiteSpace(privacy))
        {
            return "full";
        }

        var normalized = privacy.Trim().ToLowerInvariant();
        return normalized is "full" or "redacted"
            ? normalized
            : throw new ArgumentException("Privacy must be either 'full' or 'redacted'.");
    }
}
