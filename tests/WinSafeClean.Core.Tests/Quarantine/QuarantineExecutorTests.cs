using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class QuarantineExecutorTests
{
    [Fact]
    public void ShouldMoveFileAndWriteRestoreMetadataWhenPreflightPasses()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.Equal("cache", fileSystem.ReadFile(QuarantinePath));
        Assert.True(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains(@"""restorePlanId"": ""abcd""", fileSystem.ReadFile(RestoreMetadataPath));
        Assert.Contains(result.OperationLog.Entries, entry => entry.OperationType == QuarantineOperationType.QuarantineCompleted);
    }

    [Fact]
    public void ShouldAppendStartedAndCompletedOperationLogWhenPathIsProvided()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        var log = fileSystem.ReadFile(OperationLogPath);
        Assert.Contains(@"""operationType"":""QuarantineStarted""", log);
        Assert.Contains(@"""operationType"":""QuarantineCompleted""", log);
    }

    [Fact]
    public void ShouldNotMoveWhenStartedOperationLogAppendFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.ThrowOnAppendPath = OperationLogPath;
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("operation log", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldKeepQuarantinedFileWhenCompletedOperationLogAppendFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.ThrowOnAppendContaining = "QuarantineCompleted";
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.True(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("operation log", result.WarningMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldNotMoveFileWhenPreflightFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: false,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains(result.PreflightChecklist.Checks, check => check.Code == "ManualConfirmation" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenSourceFileIsMissingWithoutCreatingTarget()
    {
        var fileSystem = new FakeQuarantineFileSystem();
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("source file", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldNotMoveWhenRestoreMetadataWriteFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.ThrowOnWritePath = RestoreMetadataPath;
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("metadata write failed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldDeleteRestoreMetadataWhenMoveFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.ThrowOnMoveSourcePath = SourcePath;
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("metadata removed", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldFailWhenQuarantineTargetAlreadyExists()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.AddFile(QuarantinePath, "existing");
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.Equal("existing", fileSystem.ReadFile(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
    }

    [Fact]
    public void ShouldFailWhenRestoreMetadataAlreadyExists()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(SourcePath, "cache");
        fileSystem.AddFile(RestoreMetadataPath, "existing metadata");
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.Equal("existing metadata", fileSystem.ReadFile(RestoreMetadataPath));
    }

    [Fact]
    public void ShouldFailWhenSourceIsDirectory()
    {
        var fileSystem = new FakeQuarantineFileSystem();
        fileSystem.AddDirectory(SourcePath);
        var executor = new QuarantineExecutor(fileSystem);

        var result = executor.Execute(
            CreatePlan(),
            CreateMetadata(),
            new QuarantineExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.False(fileSystem.FileExists(RestoreMetadataPath));
        Assert.Contains("directory", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    private const string QuarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
    private const string SourcePath = @"C:\Users\Alice\AppData\Local\Example\cache.tmp";
    private static readonly string QuarantinePath = Path.Combine(QuarantineRoot, "items", "abcd-cache.tmp");
    private static readonly string RestoreMetadataPath = Path.Combine(QuarantineRoot, "restore", "abcd.restore.json");
    private static readonly string OperationLogPath = Path.Combine(QuarantineRoot, "logs", "operations.jsonl");

    private static CleanupPlan CreatePlan()
    {
        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            QuarantineRoot: QuarantineRoot,
            Items:
            [
                new CleanupPlanItem(
                    Path: SourcePath,
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: SourcePath,
                        ProposedQuarantinePath: QuarantinePath,
                        RestoreMetadataPath: RestoreMetadataPath,
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: ["Quarantine preview only; no file operation has been executed."]))
            ]);
    }

    private static RestoreMetadata CreateMetadata()
    {
        return new RestoreMetadata(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            CleanupPlanSchemaVersion: "0.2",
            RestorePlanId: "abcd",
            OriginalPath: SourcePath,
            QuarantinePath: QuarantinePath,
            RestoreMetadataPath: RestoreMetadataPath,
            RiskLevel: RiskLevel.LowRisk,
            PlanAction: CleanupPlanAction.ReviewForQuarantine,
            Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
            Warnings: ["Quarantine preview only; no file operation has been executed."],
            RequiresManualConfirmation: true,
            Redacted: false);
    }

    private sealed class FakeQuarantineFileSystem : IQuarantineFileSystem
    {
        private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

        public string? ThrowOnWritePath { get; set; }
        public string? ThrowOnMoveSourcePath { get; set; }
        public string? ThrowOnAppendPath { get; set; }
        public string? ThrowOnAppendContaining { get; set; }

        public static FakeQuarantineFileSystem WithFile(string path, string contents)
        {
            var fileSystem = new FakeQuarantineFileSystem();
            fileSystem.files[path] = contents;
            return fileSystem;
        }

        public void AddFile(string path, string contents)
        {
            files[path] = contents;
        }

        public void AddDirectory(string path)
        {
            directories.Add(path);
        }

        public bool FileExists(string path)
        {
            return files.ContainsKey(path);
        }

        public bool DirectoryExists(string path)
        {
            return directories.Contains(path);
        }

        public void CreateDirectory(string path)
        {
            directories.Add(path);
        }

        public void MoveFile(string sourcePath, string targetPath)
        {
            if (sourcePath.Equals(ThrowOnMoveSourcePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Move failed.");
            }

            if (!files.Remove(sourcePath, out var contents))
            {
                throw new FileNotFoundException("Missing source.", sourcePath);
            }

            if (files.ContainsKey(targetPath))
            {
                throw new IOException("Target exists.");
            }

            files[targetPath] = contents;
        }

        public void WriteNewTextFile(string path, string contents)
        {
            if (path.Equals(ThrowOnWritePath, StringComparison.OrdinalIgnoreCase))
            {
                throw new IOException("Write failed.");
            }

            if (files.ContainsKey(path))
            {
                throw new IOException("File exists.");
            }

            files[path] = contents;
        }

        public void DeleteFileIfExists(string path)
        {
            files.Remove(path);
        }

        public void AppendTextFile(string path, string contents)
        {
            if (path.Equals(ThrowOnAppendPath, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrWhiteSpace(ThrowOnAppendContaining)
                    && contents.Contains(ThrowOnAppendContaining, StringComparison.OrdinalIgnoreCase)))
            {
                throw new IOException("Append failed.");
            }

            files.TryGetValue(path, out var existing);
            files[path] = existing + contents;
        }

        public string ReadFile(string path)
        {
            return files[path];
        }
    }
}
