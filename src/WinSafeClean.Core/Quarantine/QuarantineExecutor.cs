using WinSafeClean.Core.Planning;

namespace WinSafeClean.Core.Quarantine;

public sealed class QuarantineExecutor
{
    private const string OperationLogSchemaVersion = "1.0";
    private readonly IQuarantineFileSystem fileSystem;

    public QuarantineExecutor()
        : this(new SystemQuarantineFileSystem())
    {
    }

    public QuarantineExecutor(IQuarantineFileSystem fileSystem)
    {
        ArgumentNullException.ThrowIfNull(fileSystem);

        this.fileSystem = fileSystem;
    }

    public QuarantineExecutionResult Execute(
        CleanupPlan plan,
        RestoreMetadata metadata,
        QuarantineExecutionOptions options,
        DateTimeOffset timestamp)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(metadata);
        ArgumentNullException.ThrowIfNull(options);

        var preflightChecklist = QuarantinePreflightValidator.Validate(
            plan,
            metadata,
            timestamp,
            options.ManualConfirmationProvided);

        if (!preflightChecklist.IsExecutable)
        {
            return Failure(
                preflightChecklist,
                options,
                timestamp,
                "Quarantine preflight did not pass.",
                QuarantineOperationStatus.Skipped);
        }

        var sourcePath = metadata.OriginalPath;
        var quarantinePath = metadata.QuarantinePath;
        var restoreMetadataPath = metadata.RestoreMetadataPath;

        if (fileSystem.DirectoryExists(sourcePath))
        {
            return Failure(preflightChecklist, options, timestamp, "Directory quarantine is not supported by the minimal executor.", QuarantineOperationStatus.Failed);
        }

        if (!fileSystem.FileExists(sourcePath))
        {
            return Failure(preflightChecklist, options, timestamp, "Source file does not exist.", QuarantineOperationStatus.Failed);
        }

        if (fileSystem.FileExists(quarantinePath))
        {
            return Failure(preflightChecklist, options, timestamp, "Quarantine target already exists.", QuarantineOperationStatus.Failed);
        }

        if (fileSystem.FileExists(restoreMetadataPath))
        {
            return Failure(preflightChecklist, options, timestamp, "Restore metadata target already exists.", QuarantineOperationStatus.Failed);
        }

        try
        {
            CreateParentDirectory(quarantinePath);
            CreateParentDirectory(restoreMetadataPath);

            try
            {
                fileSystem.WriteNewTextFile(restoreMetadataPath, RestoreMetadataJsonSerializer.Serialize(metadata));
            }
            catch (Exception metadataException)
            {
                fileSystem.DeleteFileIfExists(restoreMetadataPath);
                return Failure(
                    preflightChecklist,
                    options,
                    timestamp,
                    $"Restore metadata write failed; source was not moved. {metadataException.Message}",
                    QuarantineOperationStatus.Failed);
            }

            try
            {
                fileSystem.MoveFile(sourcePath, quarantinePath);
            }
            catch (Exception moveException)
            {
                try
                {
                    fileSystem.DeleteFileIfExists(restoreMetadataPath);
                    return Failure(
                        preflightChecklist,
                        options,
                        timestamp,
                        $"Quarantine move failed; restore metadata removed. {moveException.Message}",
                        QuarantineOperationStatus.Failed);
                }
                catch (Exception cleanupException)
                {
                    return Failure(
                        preflightChecklist,
                        options,
                        timestamp,
                        $"Quarantine move failed and restore metadata cleanup failed. {cleanupException.Message}",
                        QuarantineOperationStatus.Failed);
                }
            }

            var log = CreateOperationLog(
                options,
                timestamp,
                QuarantineOperationType.QuarantineCompleted,
                QuarantineOperationStatus.Succeeded,
                sourcePath,
                quarantinePath,
                restoreMetadataPath,
                "Quarantine completed.");
            return new QuarantineExecutionResult(
                Succeeded: true,
                PreflightChecklist: preflightChecklist,
                OperationLog: log,
                ErrorMessage: null);
        }
        catch (Exception exception)
        {
            return Failure(preflightChecklist, options, timestamp, exception.Message, QuarantineOperationStatus.Failed);
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

    private static QuarantineExecutionResult Failure(
        QuarantinePreflightChecklist checklist,
        QuarantineExecutionOptions options,
        DateTimeOffset timestamp,
        string message,
        QuarantineOperationStatus status)
    {
        var log = CreateOperationLog(
            options,
            timestamp,
            QuarantineOperationType.QuarantineFailed,
            status,
            SourcePath: string.Empty,
            TargetPath: null,
            RestoreMetadataPath: null,
            message);

        return new QuarantineExecutionResult(
            Succeeded: false,
            PreflightChecklist: checklist,
            OperationLog: log,
            ErrorMessage: message);
    }

    private static QuarantineOperationLog CreateOperationLog(
        QuarantineExecutionOptions options,
        DateTimeOffset timestamp,
        QuarantineOperationType operationType,
        QuarantineOperationStatus status,
        string SourcePath,
        string? TargetPath,
        string? RestoreMetadataPath,
        string message)
    {
        return new QuarantineOperationLog(
            SchemaVersion: OperationLogSchemaVersion,
            CreatedAt: timestamp,
            Entries:
            [
                new QuarantineOperationLogEntry(
                    OperationId: options.OperationId,
                    RunId: options.RunId,
                    RestorePlanId: string.Empty,
                    OperationType: operationType,
                    Status: status,
                    Timestamp: timestamp,
                    SourcePath: SourcePath,
                    TargetPath: TargetPath,
                    RestoreMetadataPath: RestoreMetadataPath,
                    IsDryRun: false,
                    Actor: "WinSafeClean.Core",
                    Message: message)
            ]);
    }
}
