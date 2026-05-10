using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;
using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class PlanPreflightPreparationTests
{
    [Fact]
    public void CreateRestoreMetadataForPlanItem_ReturnsMatchingPreviewMetadata()
    {
        var createdAt = new DateTimeOffset(2026, 5, 10, 15, 20, 0, TimeSpan.Zero);
        var plan = CreatePlan();

        var metadata = PlanPreflightPreparation.CreateRestoreMetadataForPlanItem(
            plan,
            @"C:\Temp\cache.tmp",
            @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\cache.restore.json",
            createdAt);

        Assert.Equal("1.0", metadata.SchemaVersion);
        Assert.Equal(createdAt, metadata.CreatedAt);
        Assert.Equal(@"C:\Temp\cache.tmp", metadata.OriginalPath);
        Assert.Equal(@"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\cache.tmp", metadata.QuarantinePath);
        Assert.Equal(@"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\cache.restore.json", metadata.RestoreMetadataPath);
        Assert.True(metadata.RequiresManualConfirmation);
    }

    [Fact]
    public void CreateRestoreMetadataForPlanItem_RejectsItemsWithoutPreview()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlanPreflightPreparation.CreateRestoreMetadataForPlanItem(
                plan,
                @"C:\Temp\keep.txt",
                restoreMetadataPath: null,
                DateTimeOffset.UtcNow));

        Assert.Equal("Selected plan item does not have a quarantine preview.", exception.Message);
    }

    [Fact]
    public void CreateRestoreMetadataForPlanItem_RejectsMissingPlanItem()
    {
        var plan = CreatePlan();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            PlanPreflightPreparation.CreateRestoreMetadataForPlanItem(
                plan,
                @"C:\Temp\missing.tmp",
                @"C:\missing.restore.json",
                DateTimeOffset.UtcNow));

        Assert.Equal("Selected plan item was not found in the loaded cleanup plan.", exception.Message);
    }

    private static CleanupPlan CreatePlan()
    {
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";

        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.Parse("2026-05-10T15:00:00Z"),
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Temp\keep.txt",
                    Action: CleanupPlanAction.Keep,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Referenced by evidence."]),
                new CleanupPlanItem(
                    Path: @"C:\Temp\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.SafeCandidate,
                    Reasons: ["Known cleanup rule matched."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Temp\cache.tmp",
                        ProposedQuarantinePath: Path.Combine(quarantineRoot, "items", "cache.tmp"),
                        RestoreMetadataPath: Path.Combine(quarantineRoot, "restore", "cache.restore.json"),
                        RestorePlanId: "cache",
                        RequiresManualConfirmation: true,
                        Warnings: ["Manual confirmation is required."]))
            ],
            QuarantineRoot: quarantineRoot);
    }
}
