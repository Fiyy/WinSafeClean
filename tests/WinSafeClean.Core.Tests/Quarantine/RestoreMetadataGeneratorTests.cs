using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class RestoreMetadataGeneratorTests
{
    [Fact]
    public void ShouldCreateRestoreMetadataOnlyForQuarantinePreviewItems()
    {
        var plan = new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            QuarantineRoot: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine",
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                        ProposedQuarantinePath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp",
                        RestoreMetadataPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json",
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: ["Quarantine preview only; no file operation has been executed."])),
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\Downloads\unknown.bin",
                    Action: CleanupPlanAction.ReportOnly,
                    RiskLevel: RiskLevel.Unknown,
                    Reasons: ["Insufficient evidence for a cleanup action; report only."])
            ]);

        var metadata = RestoreMetadataGenerator.Generate(plan, new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero));

        var item = Assert.Single(metadata);
        Assert.Equal("1.0", item.SchemaVersion);
        Assert.Equal("0.2", item.CleanupPlanSchemaVersion);
        Assert.Equal("abcd", item.RestorePlanId);
        Assert.Equal(@"C:\Users\Alice\AppData\Local\Example\cache.tmp", item.OriginalPath);
        Assert.Equal(@"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp", item.QuarantinePath);
        Assert.Equal(@"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json", item.RestoreMetadataPath);
        Assert.Equal(RiskLevel.LowRisk, item.RiskLevel);
        Assert.Equal(CleanupPlanAction.ReviewForQuarantine, item.PlanAction);
        Assert.True(item.RequiresManualConfirmation);
        Assert.False(item.Redacted);
        Assert.Contains("manual review", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no file operation", string.Join(" ", item.Warnings), StringComparison.OrdinalIgnoreCase);
    }
}
