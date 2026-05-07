using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class RestoreMetadataSchemaFixtureTests
{
    [Fact]
    public void ShouldMatchVersion10JsonFixture()
    {
        var metadata = CreateMetadata();

        var json = RestoreMetadataJsonSerializer.Serialize(metadata);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Quarantine", "fixtures", "restore-metadata-v1.0.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    [Fact]
    public void ShouldMatchVersion11JsonFixtureWithContentHash()
    {
        var metadata = CreateMetadata() with
        {
            SchemaVersion = "1.1",
            ContentHashAlgorithm = "SHA256",
            ContentHash = "5e1ecee06a7fc06f305ae5c12acfe7a7f67b8ece7af76932ed3afab00c3c6921"
        };

        var json = RestoreMetadataJsonSerializer.Serialize(metadata);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Quarantine", "fixtures", "restore-metadata-v1.1.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    [Fact]
    public void ShouldDeserializeRestoreMetadataJsonWithReadableEnumValues()
    {
        var json = RestoreMetadataJsonSerializer.Serialize(CreateMetadata());

        var metadata = RestoreMetadataJsonSerializer.Deserialize(json);

        Assert.Equal("1.0", metadata.SchemaVersion);
        Assert.Equal(RiskLevel.LowRisk, metadata.RiskLevel);
        Assert.Equal(CleanupPlanAction.ReviewForQuarantine, metadata.PlanAction);
    }

    [Fact]
    public void ShouldDeserializeRestoreMetadataJsonWithContentHash()
    {
        var json = RestoreMetadataJsonSerializer.Serialize(CreateMetadata() with
        {
            SchemaVersion = "1.1",
            ContentHashAlgorithm = "SHA256",
            ContentHash = "5e1ecee06a7fc06f305ae5c12acfe7a7f67b8ece7af76932ed3afab00c3c6921"
        });

        var metadata = RestoreMetadataJsonSerializer.Deserialize(json);

        Assert.Equal("1.1", metadata.SchemaVersion);
        Assert.Equal("SHA256", metadata.ContentHashAlgorithm);
        Assert.Equal("5e1ecee06a7fc06f305ae5c12acfe7a7f67b8ece7af76932ed3afab00c3c6921", metadata.ContentHash);
    }


    [Fact]
    public void ShouldRejectInvalidRestoreMetadataJson()
    {
        Assert.ThrowsAny<Exception>(() => RestoreMetadataJsonSerializer.Deserialize("{not valid json"));
    }

    private static RestoreMetadata CreateMetadata()
    {
        return new RestoreMetadata(
            SchemaVersion: "1.0",
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            CleanupPlanSchemaVersion: "0.2",
            RestorePlanId: "abcd",
            OriginalPath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
            QuarantinePath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp",
            RestoreMetadataPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json",
            RiskLevel: RiskLevel.LowRisk,
            PlanAction: CleanupPlanAction.ReviewForQuarantine,
            Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
            Warnings: ["Quarantine preview only; no file operation has been executed."],
            RequiresManualConfirmation: true,
            Redacted: false);
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
    }
}
