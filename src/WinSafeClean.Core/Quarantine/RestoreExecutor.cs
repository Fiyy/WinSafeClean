namespace WinSafeClean.Core.Quarantine;

public sealed class RestoreExecutor
{
    private const string OperationLogSchemaVersion = "1.0";
    private readonly IQuarantineFileSystem fileSystem;

    public RestoreExecutor()
        : this(new SystemQuarantineFileSystem())
    {
    }

    public RestoreExecutor(IQuarantineFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public RestoreExecutionResult Execute(
        RestoreMetadata metadata,
        RestoreExecutionOptions options,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(options);

        if (metadata.Redacted)
        {
            return Failure(metadata, options, timestamp, "Redacted restore metadata cannot be used for restore.", QuarantineOperationStatus.Failed);
        }

        if (metadata.RequiresManualConfirmation && !options.ManualConfirmationProvided)
        {
            return Failure(metadata, options, timestamp, "Manual confirmation is required before restore.", QuarantineOperationStatus.Failed);
        }

        if (fileSystem.DirectoryExists(metadata.QuarantinePath))
        {
            return Failure(metadata, options, timestamp, "Directory restore is not supported by the minimal executor.", QuarantineOperationStatus.Failed);
        }

        if (!fileSystem.FileExists(metadata.QuarantinePath))
        {
            return Failure(metadata, options, timestamp, "Quarantine file does not exist.", QuarantineOperationStatus.Failed);
        }

        if (!ContentHashMatches(metadata, options, out var hashFailureMessage))
        {
            return Failure(metadata, options, timestamp, hashFailureMessage, QuarantineOperationStatus.Failed);
        }

        if (fileSystem.FileExists(metadata.OriginalPath) || fileSystem.DirectoryExists(metadata.OriginalPath))
        {
            return Failure(metadata, options, timestamp, "Original path already exists; restore will not overwrite.", QuarantineOperationStatus.Failed);
        }

        try
        {
            CreateParentDirectory(metadata.OriginalPath);
            if (!string.IsNullOrWhiteSpace(options.OperationLogPath))
            {
                CreateParentDirectory(options.OperationLogPath);
                var startedEntry = CreateOperationLogEntry(
                    metadata,
                    options,
                    timestamp,
                    QuarantineOperationType.RestoreStarted,
                    QuarantineOperationStatus.Succeeded,
                    "Restore started.");
                fileSystem.AppendTextFile(options.OperationLogPath, QuarantineOperationLogJsonLinesSerializer.SerializeEntry(startedEntry));
            }

            fileSystem.MoveFile(metadata.QuarantinePath, metadata.OriginalPath);

            var log = CreateOperationLog(
                metadata,
                options,
                timestamp,
                QuarantineOperationType.RestoreCompleted,
                QuarantineOperationStatus.Succeeded,
                "Restore completed.");

            string? warningMessage = null;
            if (!string.IsNullOrWhiteSpace(options.OperationLogPath))
            {
                try
                {
                    fileSystem.AppendTextFile(
                        options.OperationLogPath,
                        QuarantineOperationLogJsonLinesSerializer.SerializeEntry(log.Entries[0]));
                }
                catch (Exception logException)
                {
                    warningMessage = $"Restore completed, but operation log append failed. {logException.Message}";
                }
            }

            return new RestoreExecutionResult(
                Succeeded: true,
                OperationLog: log,
                ErrorMessage: null,
                WarningMessage: warningMessage);
        }
        catch (Exception exception)
        {
            return Failure(metadata, options, timestamp, exception.Message, QuarantineOperationStatus.Failed);
        }
    }

    private bool ContentHashMatches(RestoreMetadata metadata, RestoreExecutionOptions options, out string failureMessage)
    {
        if (string.IsNullOrWhiteSpace(metadata.ContentHash)
            && string.IsNullOrWhiteSpace(metadata.ContentHashAlgorithm))
        {
            if (options.AllowLegacyMetadataWithoutContentHash)
            {
                failureMessage = string.Empty;
                return true;
            }

            failureMessage = "Legacy restore metadata without content hash requires explicit legacy metadata confirmation.";
            return false;
        }

        if (!string.Equals(metadata.ContentHashAlgorithm, "SHA256", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(metadata.ContentHash))
        {
            failureMessage = "Restore metadata content hash is missing or uses an unsupported algorithm.";
            return false;
        }

        try
        {
            var currentHash = fileSystem.ComputeSha256Hash(metadata.QuarantinePath);
            if (currentHash.Equals(metadata.ContentHash, StringComparison.OrdinalIgnoreCase))
            {
                failureMessage = string.Empty;
                return true;
            }

            failureMessage = "Quarantine file content hash does not match restore metadata.";
            return false;
        }
        catch
        {
            failureMessage = "Quarantine file content hash could not be verified.";
            return false;
        }
    }

    private void CreateParentDirectory(string path)
    {
        var parent = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(parent) && !fileSystem.DirectoryExists(parent))
        {
            fileSystem.CreateDirectory(parent);
        }
    }

    private static RestoreExecutionResult Failure(
        RestoreMetadata metadata,
        RestoreExecutionOptions options,
        DateTimeOffset timestamp,
        string message,
        QuarantineOperationStatus status)
    {
        return new RestoreExecutionResult(
            Succeeded: false,
            OperationLog: CreateOperationLog(
                metadata,
                options,
                timestamp,
                QuarantineOperationType.RestoreFailed,
                status,
                message),
            ErrorMessage: message);
    }

    private static QuarantineOperationLog CreateOperationLog(
        RestoreMetadata metadata,
        RestoreExecutionOptions options,
        DateTimeOffset timestamp,
        QuarantineOperationType operationType,
        QuarantineOperationStatus status,
        string message)
    {
        return new QuarantineOperationLog(
            SchemaVersion: OperationLogSchemaVersion,
            CreatedAt: timestamp,
            Entries:
            [
                CreateOperationLogEntry(metadata, options, timestamp, operationType, status, message)
            ]);
    }

    private static QuarantineOperationLogEntry CreateOperationLogEntry(
        RestoreMetadata metadata,
        RestoreExecutionOptions options,
        DateTimeOffset timestamp,
        QuarantineOperationType operationType,
        QuarantineOperationStatus status,
        string message)
    {
        return new QuarantineOperationLogEntry(
            OperationId: options.OperationId,
            RunId: options.RunId,
            RestorePlanId: metadata.RestorePlanId,
            OperationType: operationType,
            Status: status,
            Timestamp: timestamp,
            SourcePath: metadata.QuarantinePath,
            TargetPath: metadata.OriginalPath,
            RestoreMetadataPath: metadata.RestoreMetadataPath,
            IsDryRun: false,
            Actor: "WinSafeClean.Core",
            Message: message);
    }
}
