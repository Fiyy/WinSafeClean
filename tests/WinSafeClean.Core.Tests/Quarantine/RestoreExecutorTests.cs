using System.Security.Cryptography;
using System.Text;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class RestoreExecutorTests
{
    [Fact]
    public void ShouldMoveQuarantinedFileBackToOriginalPath()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.Equal("cache", fileSystem.ReadFile(SourcePath));
        Assert.Contains(result.OperationLog.Entries, entry => entry.OperationType == QuarantineOperationType.RestoreCompleted);
    }

    [Fact]
    public void ShouldRejectLegacyMetadataWithoutContentHashByDefault()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false, contentHash: null),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.Contains("legacy", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldAllowLegacyMetadataWithoutContentHashWhenExplicitlyAllowed()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false, contentHash: null),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                AllowLegacyMetadataWithoutContentHash: true),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
    }

    [Fact]
    public void ShouldFailRedactedRestoreMetadata()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: true),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.Contains("redacted", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldFailWhenManualConfirmationIsMissing()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: false,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.Contains("confirmation", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldFailWhenOriginalPathAlreadyExists()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        fileSystem.AddFile(SourcePath, "existing");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.Equal("existing", fileSystem.ReadFile(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
    }

    [Fact]
    public void ShouldFailWhenQuarantinedContentHashDoesNotMatchMetadata()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "tampered");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false, contentHash: ComputeSha256Hex("cache")),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001"),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
        Assert.Contains("hash", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldAppendRestoreOperationLogWhenPathIsProvided()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        var log = fileSystem.ReadFile(OperationLogPath);
        Assert.Contains("RestoreStarted", log);
        Assert.Contains("RestoreCompleted", log);
    }

    [Fact]
    public void ShouldNotMoveWhenStartedOperationLogAppendFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        fileSystem.ThrowOnAppendPath = OperationLogPath;
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.False(result.Succeeded);
        Assert.False(fileSystem.FileExists(SourcePath));
        Assert.True(fileSystem.FileExists(QuarantinePath));
    }

    [Fact]
    public void ShouldKeepRestoredFileWhenCompletedOperationLogAppendFails()
    {
        var fileSystem = FakeQuarantineFileSystem.WithFile(QuarantinePath, "cache");
        fileSystem.ThrowOnAppendContaining = "RestoreCompleted";
        var executor = new RestoreExecutor(fileSystem);

        var result = executor.Execute(
            CreateMetadata(redacted: false),
            new RestoreExecutionOptions(
                ManualConfirmationProvided: true,
                OperationId: "op-001",
                RunId: "run-001",
                OperationLogPath: OperationLogPath),
            DateTimeOffset.UnixEpoch);

        Assert.True(result.Succeeded);
        Assert.True(fileSystem.FileExists(SourcePath));
        Assert.False(fileSystem.FileExists(QuarantinePath));
        Assert.Contains("operation log", result.WarningMessage, StringComparison.OrdinalIgnoreCase);
    }

    private const string QuarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
    private const string SourcePath = @"C:\Users\Alice\AppData\Local\Example\cache.tmp";
    private static readonly string QuarantinePath = Path.Combine(QuarantineRoot, "items", "abcd-cache.tmp");
    private static readonly string RestoreMetadataPath = Path.Combine(QuarantineRoot, "restore", "abcd.restore.json");
    private static readonly string OperationLogPath = Path.Combine(QuarantineRoot, "logs", "operations.jsonl");
    private const string ContentHash = "5e1ecee06a7fc06f305ae5c12acfe7a7f67b8ece7af76932ed3afab00c3c6921";

    private static RestoreMetadata CreateMetadata(bool redacted, string? contentHash = ContentHash)
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
            Redacted: redacted,
            ContentHashAlgorithm: string.IsNullOrWhiteSpace(contentHash) ? null : "SHA256",
            ContentHash: contentHash);
    }

    private sealed class FakeQuarantineFileSystem : IQuarantineFileSystem
    {
        private readonly Dictionary<string, string> files = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> directories = new(StringComparer.OrdinalIgnoreCase);

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
            if (files.ContainsKey(path))
            {
                throw new IOException("File exists.");
            }

            files[path] = contents;
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

        public string ComputeSha256Hash(string path)
        {
            if (!files.TryGetValue(path, out var contents))
            {
                throw new FileNotFoundException("Missing source.", path);
            }

            return ComputeSha256Hex(contents);
        }

        public void DeleteFileIfExists(string path)
        {
            files.Remove(path);
        }

        public string ReadFile(string path)
        {
            return files[path];
        }
    }

    private static string ComputeSha256Hex(string contents)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(contents));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
