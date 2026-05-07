namespace WinSafeClean.Ui.Operations;

public static class ReadOnlyOperationCommandBuilder
{
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
