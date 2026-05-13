namespace WinSafeClean.Ui.Operations;

public sealed record GuardedQuarantineCommandOptions(
    string PlanPath,
    string MetadataPath,
    bool ManualConfirmation,
    bool UnderstandsFileMoves,
    string? OperationLogPath = null);

public sealed record GuardedRestoreCommandOptions(
    string MetadataPath,
    bool ManualConfirmation,
    bool UnderstandsFileMoves,
    string? OperationLogPath = null,
    bool AllowLegacyMetadataWithoutHash = false);

public static class GuardedFileMoveCommandBuilder
{
    public static IReadOnlyList<string> BuildQuarantine(GuardedQuarantineCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.PlanPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.MetadataPath);
        RequireDoubleConfirmation(options.ManualConfirmation, options.UnderstandsFileMoves);

        var args = new List<string>
        {
            "quarantine",
            "--plan",
            options.PlanPath,
            "--metadata",
            options.MetadataPath,
            "--manual-confirmation",
            "--i-understand-this-moves-files"
        };
        AddOperationLog(args, options.OperationLogPath);

        return args;
    }

    public static IReadOnlyList<string> BuildRestore(GuardedRestoreCommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.MetadataPath);
        RequireDoubleConfirmation(options.ManualConfirmation, options.UnderstandsFileMoves);

        var args = new List<string>
        {
            "restore",
            "--metadata",
            options.MetadataPath,
            "--manual-confirmation",
            "--i-understand-this-moves-files"
        };

        if (options.AllowLegacyMetadataWithoutHash)
        {
            args.Add("--allow-legacy-metadata-without-hash");
        }

        AddOperationLog(args, options.OperationLogPath);

        return args;
    }

    private static void RequireDoubleConfirmation(bool manualConfirmation, bool understandsFileMoves)
    {
        if (!manualConfirmation)
        {
            throw new InvalidOperationException("Manual confirmation must be checked before building a file-moving CLI command.");
        }

        if (!understandsFileMoves)
        {
            throw new InvalidOperationException("File move acknowledgement must be checked before building a file-moving CLI command.");
        }
    }

    private static void AddOperationLog(List<string> args, string? operationLogPath)
    {
        if (string.IsNullOrWhiteSpace(operationLogPath))
        {
            return;
        }

        args.Add("--operation-log");
        args.Add(operationLogPath);
    }
}
