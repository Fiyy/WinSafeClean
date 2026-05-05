using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Cli;

public static class CommandLineApp
{
    private const int Success = 0;
    private const int UsageError = 2;
    private static readonly string[] ExecutableCommands = ["delete", "clean", "quarantine", "restore", "plan"];
    private static readonly string[] ExecutableOptions = ["--delete", "--fix", "--quarantine", "--clean"];

    public static int Run(string[] args, TextWriter stdout, TextWriter stderr, DateTimeOffset? now = null)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (args.Length == 0)
        {
            stderr.WriteLine("A command is required. Use: scan --path <PATH> [--format json|markdown] [--output <FILE>]");
            return UsageError;
        }

        var command = args[0];
        if (ExecutableCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
        {
            stderr.WriteLine($"The '{command}' command is not available during the read-only MVP phase.");
            return UsageError;
        }

        if (!command.Equals("scan", StringComparison.OrdinalIgnoreCase))
        {
            stderr.WriteLine($"Unknown command '{command}'. Use: scan --path <PATH> [--format json|markdown] [--output <FILE>]");
            return UsageError;
        }

        if (args.Any(arg => ExecutableOptions.Contains(arg, StringComparer.OrdinalIgnoreCase)))
        {
            stderr.WriteLine("Executable cleanup options are not available during the read-only MVP phase.");
            return UsageError;
        }

        return RunScan(args[1..], stdout, stderr, now ?? DateTimeOffset.UtcNow);
    }

    private static int RunScan(string[] args, TextWriter stdout, TextWriter stderr, DateTimeOffset createdAt)
    {
        var options = ScanOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        var report = BuildSingleItemReport(options.Path!, createdAt);
        var rendered = options.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
            ? ScanReportMarkdownSerializer.Serialize(report)
            : ScanReportJsonSerializer.Serialize(report);

        if (!string.IsNullOrWhiteSpace(options.OutputPath))
        {
            var outputValidationError = ValidateOutputPath(options.OutputPath);
            if (!string.IsNullOrWhiteSpace(outputValidationError))
            {
                stderr.WriteLine(outputValidationError);
                return UsageError;
            }

            using var stream = new FileStream(options.OutputPath, FileMode.CreateNew, FileAccess.Write, FileShare.None);
            using var writer = new StreamWriter(stream);
            writer.Write(rendered);
            return Success;
        }

        stdout.Write(rendered);
        return Success;
    }

    private static ScanReport BuildSingleItemReport(string path, DateTimeOffset createdAt)
    {
        var sizeBytes = File.Exists(path) ? new FileInfo(path).Length : 0;

        return new ScanReport(
            SchemaVersion: "1.0",
            CreatedAt: createdAt,
            Items:
            [
                new ScanReportItem(
                    Path: path,
                    SizeBytes: sizeBytes,
                    Risk: PathRiskClassifier.Assess(path))
            ]);
    }

    private static string? ValidateOutputPath(string outputPath)
    {
        var outputRisk = PathRiskClassifier.Assess(outputPath);
        if (outputRisk.Level == RiskLevel.Blocked)
        {
            return "--output must not target a protected Windows path.";
        }

        if (File.Exists(outputPath) || Directory.Exists(outputPath))
        {
            return "--output must not overwrite existing files or directories.";
        }

        var parent = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrWhiteSpace(parent) && !Directory.Exists(parent))
        {
            return "--output parent directory does not exist.";
        }

        return null;
    }

    private sealed record ScanOptions(string? Path, string Format, string? OutputPath, string? Error)
    {
        public static ScanOptions Parse(string[] args)
        {
            string? path = null;
            var format = "json";
            string? outputPath = null;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg)
                {
                    case "--path":
                        if (!TryReadValue(args, ref index, "--path", out path, out var pathError))
                        {
                            return Invalid(pathError);
                        }

                        break;
                    case "--format":
                        if (!TryReadValue(args, ref index, "--format", out format, out var formatError))
                        {
                            return Invalid(formatError);
                        }

                        if (!format.Equals("json", StringComparison.OrdinalIgnoreCase)
                            && !format.Equals("markdown", StringComparison.OrdinalIgnoreCase))
                        {
                            return Invalid("--format must be either 'json' or 'markdown'.");
                        }

                        break;
                    case "--output":
                        if (!TryReadValue(args, ref index, "--output", out outputPath, out var outputError))
                        {
                            return Invalid(outputError);
                        }

                        break;
                    default:
                        return Invalid($"Unknown option '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Invalid("--path is required.");
            }

            return new ScanOptions(path, format, outputPath, Error: null);
        }

        private static bool TryReadValue(string[] args, ref int index, string option, out string value, out string error)
        {
            if (index + 1 >= args.Length || args[index + 1].StartsWith("--", StringComparison.Ordinal))
            {
                value = string.Empty;
                error = $"{option} requires a value.";
                return false;
            }

            index++;
            value = args[index];
            error = string.Empty;
            return true;
        }

        private static ScanOptions Invalid(string error)
        {
            return new ScanOptions(Path: null, Format: "json", OutputPath: null, Error: error);
        }
    }
}
