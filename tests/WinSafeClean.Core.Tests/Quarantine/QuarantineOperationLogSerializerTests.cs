using WinSafeClean.Core.Quarantine;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class QuarantineOperationLogSerializerTests
{
    [Fact]
    public void ShouldSerializeOperationLogWithReadableEnumValues()
    {
        var log = CreateLog();

        var json = QuarantineOperationLogJsonSerializer.Serialize(log);

        Assert.Contains(@"""schemaVersion"": ""1.0""", json);
        Assert.Contains(@"""operationType"": ""PlanGenerated""", json);
        Assert.Contains(@"""status"": ""Succeeded""", json);
        Assert.Contains(@"""isDryRun"": true", json);
    }

    [Fact]
    public void ShouldMatchVersion10JsonFixture()
    {
        var log = CreateLog();

        var json = QuarantineOperationLogJsonSerializer.Serialize(log);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Quarantine", "fixtures", "operation-log-v1.0.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    private static QuarantineOperationLog CreateLog()
    {
        return new QuarantineOperationLog(
            SchemaVersion: "1.0",
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            Entries:
            [
                new QuarantineOperationLogEntry(
                    OperationId: "op-001",
                    RunId: "run-001",
                    RestorePlanId: "abcd",
                    OperationType: QuarantineOperationType.PlanGenerated,
                    Status: QuarantineOperationStatus.Succeeded,
                    Timestamp: new DateTimeOffset(2026, 5, 6, 1, 2, 4, TimeSpan.Zero),
                    SourcePath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                    TargetPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp",
                    RestoreMetadataPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json",
                    IsDryRun: true,
                    Actor: "WinSafeClean.Core",
                    Message: "Operation model preview only; no file operation has been executed.")
            ]);
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
    }
}
