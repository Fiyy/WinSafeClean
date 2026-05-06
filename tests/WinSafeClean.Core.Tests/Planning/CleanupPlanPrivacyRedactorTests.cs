using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Planning;

public sealed class CleanupPlanPrivacyRedactorTests
{
    [Fact]
    public void ShouldRedactCleanupPlanPathsAndQuarantinePreviewPaths()
    {
        var plan = new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: [@"Known cleanup rule for C:\Users\Alice\AppData\Local\Example\cache.tmp"],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                        ProposedQuarantinePath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp",
                        RestoreMetadataPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json",
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: [@"Restore metadata under C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine"]))
            ],
            QuarantineRoot: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine");

        var redacted = CleanupPlanPrivacyRedactor.Redact(plan);

        var item = Assert.Single(redacted.Items);
        Assert.Equal("[redacted-path-0001]", item.Path);
        Assert.Equal("[redacted-quarantine-root]", redacted.QuarantineRoot);
        Assert.NotNull(item.QuarantinePreview);
        Assert.Equal("[redacted-path-0001]", item.QuarantinePreview.OriginalPath);
        Assert.Equal("[redacted-quarantine-path-0001]", item.QuarantinePreview.ProposedQuarantinePath);
        Assert.Equal("[redacted-restore-metadata-path-0001]", item.QuarantinePreview.RestoreMetadataPath);
        Assert.Equal("[redacted-restore-plan-id-0001]", item.QuarantinePreview.RestorePlanId);
        Assert.DoesNotContain("Alice", string.Join(" ", item.Reasons));
        Assert.DoesNotContain("Alice", string.Join(" ", item.QuarantinePreview.Warnings));
    }
}
