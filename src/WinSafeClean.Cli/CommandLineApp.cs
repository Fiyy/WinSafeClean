using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.FileInventory;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;
using WinSafeClean.CleanerRules;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Cli;

public static class CommandLineApp
{
    private const int Success = 0;
    private const int OperationFailed = 1;
    private const int UsageError = 2;
    private const int Cancelled = 130;
    private const string Usage = "Use: scan|plan --path <PATH> [--format json|markdown] [--privacy full|redacted] [--output <FILE>] [--max-items <N>] [--recursive|--no-recursive] [--cleanerml <FILE_OR_DIR>] OR preflight --plan <FILE> --metadata <FILE> [--manual-confirmation] [--format json|markdown] [--output <FILE>] OR quarantine --plan <FILE> --metadata <FILE> --manual-confirmation --i-understand-this-moves-files [--operation-log <FILE>] [--format json|markdown] [--output <FILE>] OR restore --metadata <FILE> --manual-confirmation --i-understand-this-moves-files [--allow-legacy-metadata-without-hash] [--operation-log <FILE>] [--format json|markdown] [--output <FILE>]";
    private static readonly string[] ExecutableCommands = ["delete", "clean"];
    private static readonly string[] ExecutableOptions = ["--delete", "--fix", "--quarantine", "--clean"];

    public static int Run(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset? now = null,
        IFileEvidenceProvider? evidenceProvider = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(stdout);
        ArgumentNullException.ThrowIfNull(stderr);

        if (args.Length == 0)
        {
            stderr.WriteLine("A command is required. " + Usage);
            return UsageError;
        }

        var command = args[0];
        if (ExecutableCommands.Contains(command, StringComparer.OrdinalIgnoreCase))
        {
            stderr.WriteLine($"The '{command}' command is not available during the read-only MVP phase.");
            return UsageError;
        }

        if (!command.Equals("scan", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("plan", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("preflight", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("quarantine", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("restore", StringComparison.OrdinalIgnoreCase))
        {
            stderr.WriteLine($"Unknown command '{command}'. {Usage}");
            return UsageError;
        }

        if (args.Any(arg => ExecutableOptions.Contains(arg, StringComparer.OrdinalIgnoreCase)))
        {
            stderr.WriteLine("Executable cleanup options are not available during the read-only MVP phase.");
            return UsageError;
        }

        try
        {
            var createdAt = now ?? DateTimeOffset.UtcNow;
            var provider = evidenceProvider ?? EmptyEvidenceProvider.Instance;

            if (command.Equals("preflight", StringComparison.OrdinalIgnoreCase))
            {
                return RunPreflight(args[1..], stdout, stderr, createdAt, cancellationToken);
            }

            if (command.Equals("quarantine", StringComparison.OrdinalIgnoreCase))
            {
                return RunQuarantine(args[1..], stdout, stderr, createdAt, cancellationToken);
            }

            if (command.Equals("restore", StringComparison.OrdinalIgnoreCase))
            {
                return RunRestore(args[1..], stdout, stderr, createdAt, cancellationToken);
            }

            return command.Equals("plan", StringComparison.OrdinalIgnoreCase)
                ? RunPlan(args[1..], stdout, stderr, createdAt, provider, cancellationToken)
                : RunScan(args[1..], stdout, stderr, createdAt, provider, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            stderr.WriteLine("Scan cancelled.");
            return Cancelled;
        }
    }

    private static int RunScan(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset createdAt,
        IFileEvidenceProvider evidenceProvider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = ScanOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        if (!TryCreateEvidenceProvider(options, evidenceProvider, cancellationToken, out var effectiveEvidenceProvider, out var evidenceProviderError))
        {
            stderr.WriteLine(evidenceProviderError);
            return UsageError;
        }

        var report = BuildReport(options, createdAt, effectiveEvidenceProvider, cancellationToken);
        if (options.PrivacyMode == ScanReportPrivacyMode.Redacted)
        {
            report = ScanReportPrivacyRedactor.Redact(report);
        }

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

    private static int RunPlan(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset createdAt,
        IFileEvidenceProvider evidenceProvider,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = ScanOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        if (!TryCreateEvidenceProvider(options, evidenceProvider, cancellationToken, out var effectiveEvidenceProvider, out var evidenceProviderError))
        {
            stderr.WriteLine(evidenceProviderError);
            return UsageError;
        }

        var report = BuildReport(options, createdAt, effectiveEvidenceProvider, cancellationToken);
        var plan = CleanupPlanGenerator.Generate(report, createdAt);
        if (options.PrivacyMode == ScanReportPrivacyMode.Redacted)
        {
            plan = CleanupPlanPrivacyRedactor.Redact(plan);
        }

        var rendered = options.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
            ? CleanupPlanMarkdownSerializer.Serialize(plan)
            : CleanupPlanJsonSerializer.Serialize(plan);

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

    private static int RunPreflight(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = PreflightOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        if (!File.Exists(options.PlanPath))
        {
            stderr.WriteLine("--plan must point to an existing cleanup plan JSON file.");
            return UsageError;
        }

        if (!File.Exists(options.MetadataPath))
        {
            stderr.WriteLine("--metadata must point to an existing restore metadata JSON file.");
            return UsageError;
        }

        CleanupPlan plan;
        RestoreMetadata metadata;
        try
        {
            plan = CleanupPlanJsonSerializer.Deserialize(File.ReadAllText(options.PlanPath));
            metadata = RestoreMetadataJsonSerializer.Deserialize(File.ReadAllText(options.MetadataPath));
        }
        catch (Exception exception)
        {
            stderr.WriteLine($"preflight input could not be read: {exception.Message}");
            return UsageError;
        }

        var checklist = QuarantinePreflightValidator.Validate(
            plan,
            metadata,
            createdAt,
            options.ManualConfirmationProvided);
        var rendered = options.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
            ? QuarantinePreflightChecklistMarkdownSerializer.Serialize(checklist)
            : QuarantinePreflightChecklistJsonSerializer.Serialize(checklist);

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

    private static int RunQuarantine(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = QuarantineOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        if (!options.ManualConfirmationProvided)
        {
            stderr.WriteLine("quarantine requires --manual-confirmation.");
            return UsageError;
        }

        if (!options.UnderstandsFileMove)
        {
            stderr.WriteLine("quarantine requires --i-understand-this-moves-files.");
            return UsageError;
        }

        if (!File.Exists(options.PlanPath))
        {
            stderr.WriteLine("--plan must point to an existing cleanup plan JSON file.");
            return UsageError;
        }

        if (!File.Exists(options.MetadataPath))
        {
            stderr.WriteLine("--metadata must point to an existing restore metadata JSON file.");
            return UsageError;
        }

        if (!string.IsNullOrWhiteSpace(options.OperationLogPath))
        {
            var operationLogValidationError = ValidateAppendPath(options.OperationLogPath);
            if (!string.IsNullOrWhiteSpace(operationLogValidationError))
            {
                stderr.WriteLine(operationLogValidationError);
                return UsageError;
            }
        }

        CleanupPlan plan;
        RestoreMetadata metadata;
        try
        {
            plan = CleanupPlanJsonSerializer.Deserialize(File.ReadAllText(options.PlanPath));
            metadata = RestoreMetadataJsonSerializer.Deserialize(File.ReadAllText(options.MetadataPath));
        }
        catch (Exception exception)
        {
            stderr.WriteLine($"quarantine input could not be read: {exception.Message}");
            return UsageError;
        }

        var result = new QuarantineExecutor().Execute(
            plan,
            metadata,
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: options.ManualConfirmationProvided,
                OperationId: Guid.NewGuid().ToString("N"),
                RunId: Guid.NewGuid().ToString("N"),
                OperationLogPath: options.OperationLogPath),
            createdAt);

        var rendered = options.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
            ? QuarantineExecutionResultMarkdownSerializer.Serialize(result)
            : QuarantineExecutionResultJsonSerializer.Serialize(result);

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
        }
        else
        {
            stdout.Write(rendered);
        }

        return result.Succeeded ? Success : OperationFailed;
    }

    private static int RunRestore(
        string[] args,
        TextWriter stdout,
        TextWriter stderr,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var options = RestoreOptions.Parse(args);
        if (!string.IsNullOrWhiteSpace(options.Error))
        {
            stderr.WriteLine(options.Error);
            return UsageError;
        }

        if (!options.ManualConfirmationProvided)
        {
            stderr.WriteLine("restore requires --manual-confirmation.");
            return UsageError;
        }

        if (!options.UnderstandsFileMove)
        {
            stderr.WriteLine("restore requires --i-understand-this-moves-files.");
            return UsageError;
        }

        if (!File.Exists(options.MetadataPath))
        {
            stderr.WriteLine("--metadata must point to an existing restore metadata JSON file.");
            return UsageError;
        }

        if (!string.IsNullOrWhiteSpace(options.OperationLogPath))
        {
            var operationLogValidationError = ValidateAppendPath(options.OperationLogPath);
            if (!string.IsNullOrWhiteSpace(operationLogValidationError))
            {
                stderr.WriteLine(operationLogValidationError);
                return UsageError;
            }
        }

        RestoreMetadata metadata;
        try
        {
            metadata = RestoreMetadataJsonSerializer.Deserialize(File.ReadAllText(options.MetadataPath));
        }
        catch (Exception exception)
        {
            stderr.WriteLine($"restore input could not be read: {exception.Message}");
            return UsageError;
        }

        var result = new RestoreExecutor().Execute(
            metadata,
            new RestoreExecutionOptions(
                ManualConfirmationProvided: options.ManualConfirmationProvided,
                OperationId: Guid.NewGuid().ToString("N"),
                RunId: Guid.NewGuid().ToString("N"),
                OperationLogPath: options.OperationLogPath,
                AllowLegacyMetadataWithoutContentHash: options.AllowLegacyMetadataWithoutContentHash),
            createdAt);

        var rendered = options.Format.Equals("markdown", StringComparison.OrdinalIgnoreCase)
            ? RestoreExecutionResultMarkdownSerializer.Serialize(result)
            : RestoreExecutionResultJsonSerializer.Serialize(result);

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
        }
        else
        {
            stdout.Write(rendered);
        }

        return result.Succeeded ? Success : OperationFailed;
    }

    private static ScanReport BuildReport(
        ScanOptions options,
        DateTimeOffset createdAt,
        IFileEvidenceProvider evidenceProvider,
        CancellationToken cancellationToken)
    {
        return ScanReportGenerator.Generate(
            options.Path!,
            new FileSystemScanOptions(options.MaxItems, options.Recursive, cancellationToken),
            createdAt,
            evidenceProvider);
    }

    public static IFileEvidenceProvider CreateDefaultEvidenceProvider()
    {
        return new CompositeFileEvidenceProvider(WindowsEvidenceProviderFactory.CreateDefaultProviders());
    }

    private static bool TryCreateEvidenceProvider(
        ScanOptions options,
        IFileEvidenceProvider baseEvidenceProvider,
        CancellationToken cancellationToken,
        out IFileEvidenceProvider evidenceProvider,
        out string? error)
    {
        evidenceProvider = baseEvidenceProvider;
        error = null;

        if (string.IsNullOrWhiteSpace(options.CleanerMlRulePath))
        {
            return true;
        }

        try
        {
            CleanerMlRuleSet ruleSet;
            if (File.Exists(options.CleanerMlRulePath))
            {
                ruleSet = CleanerMlRuleFileLoader.LoadFile(options.CleanerMlRulePath, cancellationToken: cancellationToken);
            }
            else if (Directory.Exists(options.CleanerMlRulePath))
            {
                ruleSet = CleanerMlRuleFileLoader.LoadDirectory(options.CleanerMlRulePath, cancellationToken: cancellationToken);
            }
            else
            {
                error = "--cleanerml must point to an existing CleanerML file or directory.";
                return false;
            }

            evidenceProvider = new CompositeFileEvidenceProvider(
            [
                baseEvidenceProvider,
                new CleanerRuleEvidenceProvider(ruleSet)
            ]);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            error = $"--cleanerml could not be loaded: {ex.Message}";
            return false;
        }
    }

    private sealed class EmptyEvidenceProvider : IFileEvidenceProvider
    {
        public static readonly EmptyEvidenceProvider Instance = new();

        private EmptyEvidenceProvider()
        {
        }

        public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return [];
        }
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

    private static string? ValidateAppendPath(string appendPath)
    {
        var appendRisk = PathRiskClassifier.Assess(appendPath);
        if (appendRisk.Level == RiskLevel.Blocked)
        {
            return "--operation-log must not target a protected Windows path.";
        }

        if (Directory.Exists(appendPath))
        {
            return "--operation-log must not target an existing directory.";
        }

        return null;
    }

    private sealed record ScanOptions(
        string? Path,
        string Format,
        string? OutputPath,
        int MaxItems,
        bool Recursive,
        ScanReportPrivacyMode PrivacyMode,
        string? CleanerMlRulePath,
        string? Error)
    {
        public static ScanOptions Parse(string[] args)
        {
            string? path = null;
            var format = "json";
            string? outputPath = null;
            var maxItems = FileSystemScanOptions.Default.MaxItems;
            var recursive = FileSystemScanOptions.Default.Recursive;
            var privacyMode = ScanReportPrivacyMode.Full;
            string? cleanerMlRulePath = null;

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
                    case "--privacy":
                        if (!TryReadValue(args, ref index, "--privacy", out var privacyModeValue, out var privacyError))
                        {
                            return Invalid(privacyError);
                        }

                        if (!TryParsePrivacyMode(privacyModeValue, out privacyMode))
                        {
                            return Invalid("--privacy must be either 'full' or 'redacted'.");
                        }

                        break;
                    case "--max-items":
                        if (!TryReadValue(args, ref index, "--max-items", out var maxItemsValue, out var maxItemsError))
                        {
                            return Invalid(maxItemsError);
                        }

                        if (!int.TryParse(maxItemsValue, out maxItems) || maxItems <= 0)
                        {
                            return Invalid("--max-items must be a positive integer.");
                        }

                        break;
                    case "--cleanerml":
                        if (!TryReadValue(args, ref index, "--cleanerml", out cleanerMlRulePath, out var cleanerMlError))
                        {
                            return Invalid(cleanerMlError);
                        }

                        break;
                    case "--no-recursive":
                        recursive = false;
                        break;
                    case "--recursive":
                        recursive = true;
                        break;
                    default:
                        return Invalid($"Unknown option '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(path))
            {
                return Invalid("--path is required.");
            }

            return new ScanOptions(path, format, outputPath, maxItems, recursive, privacyMode, cleanerMlRulePath, Error: null);
        }

        private static bool TryParsePrivacyMode(string value, out ScanReportPrivacyMode privacyMode)
        {
            if (value.Equals("full", StringComparison.OrdinalIgnoreCase))
            {
                privacyMode = ScanReportPrivacyMode.Full;
                return true;
            }

            if (value.Equals("redacted", StringComparison.OrdinalIgnoreCase))
            {
                privacyMode = ScanReportPrivacyMode.Redacted;
                return true;
            }

            privacyMode = ScanReportPrivacyMode.Full;
            return false;
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
            return new ScanOptions(
                Path: null,
                Format: "json",
                OutputPath: null,
                MaxItems: FileSystemScanOptions.Default.MaxItems,
                Recursive: FileSystemScanOptions.Default.Recursive,
                PrivacyMode: ScanReportPrivacyMode.Full,
                CleanerMlRulePath: null,
                Error: error);
        }
    }

    private sealed record PreflightOptions(
        string? PlanPath,
        string? MetadataPath,
        string Format,
        string? OutputPath,
        bool ManualConfirmationProvided,
        string? Error)
    {
        public static PreflightOptions Parse(string[] args)
        {
            string? planPath = null;
            string? metadataPath = null;
            var format = "json";
            string? outputPath = null;
            var manualConfirmationProvided = false;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg)
                {
                    case "--plan":
                        if (!TryReadValue(args, ref index, "--plan", out planPath, out var planError))
                        {
                            return Invalid(planError);
                        }

                        break;
                    case "--metadata":
                        if (!TryReadValue(args, ref index, "--metadata", out metadataPath, out var metadataError))
                        {
                            return Invalid(metadataError);
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
                    case "--manual-confirmation":
                        manualConfirmationProvided = true;
                        break;
                    default:
                        return Invalid($"Unknown option '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(planPath))
            {
                return Invalid("--plan is required.");
            }

            if (string.IsNullOrWhiteSpace(metadataPath))
            {
                return Invalid("--metadata is required.");
            }

            return new PreflightOptions(planPath, metadataPath, format, outputPath, manualConfirmationProvided, Error: null);
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

        private static PreflightOptions Invalid(string error)
        {
            return new PreflightOptions(
                PlanPath: null,
                MetadataPath: null,
                Format: "json",
                OutputPath: null,
                ManualConfirmationProvided: false,
                Error: error);
        }
    }

    private sealed record QuarantineOptions(
        string? PlanPath,
        string? MetadataPath,
        string Format,
        string? OutputPath,
        string? OperationLogPath,
        bool ManualConfirmationProvided,
        bool UnderstandsFileMove,
        string? Error)
    {
        public static QuarantineOptions Parse(string[] args)
        {
            string? planPath = null;
            string? metadataPath = null;
            var format = "json";
            string? outputPath = null;
            string? operationLogPath = null;
            var manualConfirmationProvided = false;
            var understandsFileMove = false;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg)
                {
                    case "--plan":
                        if (!TryReadValue(args, ref index, "--plan", out planPath, out var planError))
                        {
                            return Invalid(planError);
                        }

                        break;
                    case "--metadata":
                        if (!TryReadValue(args, ref index, "--metadata", out metadataPath, out var metadataError))
                        {
                            return Invalid(metadataError);
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
                    case "--operation-log":
                        if (!TryReadValue(args, ref index, "--operation-log", out operationLogPath, out var operationLogError))
                        {
                            return Invalid(operationLogError);
                        }

                        break;
                    case "--manual-confirmation":
                        manualConfirmationProvided = true;
                        break;
                    case "--i-understand-this-moves-files":
                        understandsFileMove = true;
                        break;
                    default:
                        return Invalid($"Unknown option '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(planPath))
            {
                return Invalid("--plan is required.");
            }

            if (string.IsNullOrWhiteSpace(metadataPath))
            {
                return Invalid("--metadata is required.");
            }

            return new QuarantineOptions(
                planPath,
                metadataPath,
                format,
                outputPath,
                operationLogPath,
                manualConfirmationProvided,
                understandsFileMove,
                Error: null);
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

        private static QuarantineOptions Invalid(string error)
        {
            return new QuarantineOptions(
                PlanPath: null,
                MetadataPath: null,
                Format: "json",
                OutputPath: null,
                OperationLogPath: null,
                ManualConfirmationProvided: false,
                UnderstandsFileMove: false,
                Error: error);
        }
    }

    private sealed record RestoreOptions(
        string? MetadataPath,
        string Format,
        string? OutputPath,
        string? OperationLogPath,
        bool ManualConfirmationProvided,
        bool UnderstandsFileMove,
        bool AllowLegacyMetadataWithoutContentHash,
        string? Error)
    {
        public static RestoreOptions Parse(string[] args)
        {
            string? metadataPath = null;
            var format = "json";
            string? outputPath = null;
            string? operationLogPath = null;
            var manualConfirmationProvided = false;
            var understandsFileMove = false;
            var allowLegacyMetadataWithoutContentHash = false;

            for (var index = 0; index < args.Length; index++)
            {
                var arg = args[index];
                switch (arg)
                {
                    case "--metadata":
                        if (!TryReadValue(args, ref index, "--metadata", out metadataPath, out var metadataError))
                        {
                            return Invalid(metadataError);
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
                    case "--operation-log":
                        if (!TryReadValue(args, ref index, "--operation-log", out operationLogPath, out var operationLogError))
                        {
                            return Invalid(operationLogError);
                        }

                        break;
                    case "--manual-confirmation":
                        manualConfirmationProvided = true;
                        break;
                    case "--i-understand-this-moves-files":
                        understandsFileMove = true;
                        break;
                    case "--allow-legacy-metadata-without-hash":
                        allowLegacyMetadataWithoutContentHash = true;
                        break;
                    default:
                        return Invalid($"Unknown option '{arg}'.");
                }
            }

            if (string.IsNullOrWhiteSpace(metadataPath))
            {
                return Invalid("--metadata is required.");
            }

            return new RestoreOptions(
                metadataPath,
                format,
                outputPath,
                operationLogPath,
                manualConfirmationProvided,
                understandsFileMove,
                allowLegacyMetadataWithoutContentHash,
                Error: null);
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

        private static RestoreOptions Invalid(string error)
        {
            return new RestoreOptions(
                MetadataPath: null,
                Format: "json",
                OutputPath: null,
                OperationLogPath: null,
                ManualConfirmationProvided: false,
                UnderstandsFileMove: false,
                AllowLegacyMetadataWithoutContentHash: false,
                Error: error);
        }
    }
}
