using System.Globalization;

namespace WinSafeClean.Ui.Operations;

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
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var args = new List<string> { "scan", "--path", path };
        if (recursive)
        {
            args.Add("--recursive");
        }

        if (maxItems is not null)
        {
            if (maxItems <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxItems), "Max items must be positive.");
            }

            args.Add("--max-items");
            args.Add(maxItems.Value.ToString());
        }

        return args;
    }

    public static IReadOnlyList<string> BuildPlan(
        string path,
        string? cleanerMlPath = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var args = new List<string> { "plan", "--path", path };
        if (!string.IsNullOrWhiteSpace(cleanerMlPath))
        {
            args.Add("--cleanerml");
            args.Add(cleanerMlPath);
        }

        return args;
    }

    public static IReadOnlyList<string> BuildPreflight(
        string planPath,
        string metadataPath,
        bool manualConfirmation = false)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(planPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(metadataPath);

        var args = new List<string> { "preflight", "--plan", planPath, "--metadata", metadataPath };
        if (manualConfirmation)
        {
            args.Add("--manual-confirmation");
        }

        return args;
    }
}
