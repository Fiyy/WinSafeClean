using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Quarantine;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class QuarantinePreflightValidatorTests
{
    [Fact]
    public void ShouldPassWhenMetadataMatchesReviewPlanAndConfirmationProvided()
    {
        var plan = CreatePlan();
        var metadata = CreateMetadata(redacted: false);

        var checklist = QuarantinePreflightValidator.Validate(
            plan,
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.True(checklist.IsExecutable);
        Assert.All(checklist.Checks, check => Assert.NotEqual(QuarantinePreflightCheckStatus.Failed, check.Status));
        Assert.Contains(checklist.Checks, check => check.Code == "ManualConfirmation" && check.Status == QuarantinePreflightCheckStatus.Passed);
    }

    [Fact]
    public void ShouldFailRedactedRestoreMetadata()
    {
        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(),
            CreateMetadata(redacted: true),
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "RestoreMetadataNotRedacted" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenManualConfirmationIsMissing()
    {
        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(),
            CreateMetadata(redacted: false),
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: false);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "ManualConfirmation" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenQuarantinePathEscapesQuarantineRoot()
    {
        var metadata = CreateMetadata(redacted: false) with
        {
            QuarantinePath = @"C:\Users\Alice\AppData\Local\Other\items\abcd-cache.tmp"
        };

        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(),
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "PathsRemainInsideQuarantineRoot" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailProtectedQuarantineRoot()
    {
        var plan = CreatePlan() with
        {
            QuarantineRoot = @"C:\Windows\Installer"
        };

        var checklist = QuarantinePreflightValidator.Validate(
            plan,
            CreateMetadata(redacted: false),
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "QuarantineRootAllowed" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailUnsupportedCleanupPlanSchema()
    {
        var plan = CreatePlan() with
        {
            SchemaVersion = "0.1"
        };

        var checklist = QuarantinePreflightValidator.Validate(
            plan,
            CreateMetadata(redacted: false),
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "CleanupPlanSchemaSupported" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailRedactedPathTokens()
    {
        var metadata = CreateMetadata(redacted: false) with
        {
            OriginalPath = "[redacted-path-0001]"
        };

        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(),
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "PathsAreFullFidelity" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenSourceIsAlreadyInsideQuarantineRoot()
    {
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
        var metadata = CreateMetadata(redacted: false, quarantineRoot) with
        {
            OriginalPath = Path.Combine(quarantineRoot, "items", "already-quarantined.tmp")
        };

        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(quarantineRoot),
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "SourceOutsideQuarantineRoot" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenPreviewDoesNotRequireManualConfirmation()
    {
        var plan = CreatePlan(previewRequiresManualConfirmation: false);

        var checklist = QuarantinePreflightValidator.Validate(
            plan,
            CreateMetadata(redacted: false),
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "PreviewRequiresManualConfirmation" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldFailWhenRestoreMetadataPathMatchesQuarantinePath()
    {
        var metadata = CreateMetadata(redacted: false) with
        {
            RestoreMetadataPath = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp"
        };

        var checklist = QuarantinePreflightValidator.Validate(
            CreatePlan(),
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(checklist.IsExecutable);
        Assert.Contains(checklist.Checks, check => check.Code == "NoTargetMetadataOverlap" && check.Status == QuarantinePreflightCheckStatus.Failed);
    }

    [Fact]
    public void ShouldNotCreateQuarantineRoot()
    {
        var quarantineRoot = Path.Combine(Path.GetTempPath(), "WinSafeClean.Core.Tests", Path.GetRandomFileName());
        var plan = CreatePlan(quarantineRoot);
        var metadata = CreateMetadata(redacted: false, quarantineRoot);
        Assert.False(Directory.Exists(quarantineRoot));

        QuarantinePreflightValidator.Validate(
            plan,
            metadata,
            DateTimeOffset.UnixEpoch,
            manualConfirmationProvided: true);

        Assert.False(Directory.Exists(quarantineRoot));
    }

    private static CleanupPlan CreatePlan(
        string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine",
        bool previewRequiresManualConfirmation = true)
    {
        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: DateTimeOffset.UnixEpoch,
            QuarantineRoot: quarantineRoot,
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                        ProposedQuarantinePath: Path.Combine(quarantineRoot, "items", "abcd-cache.tmp"),
                        RestoreMetadataPath: Path.Combine(quarantineRoot, "restore", "abcd.restore.json"),
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: previewRequiresManualConfirmation,
                        Warnings: ["Quarantine preview only; no file operation has been executed."]))
            ]);
    }

    private static RestoreMetadata CreateMetadata(
        bool redacted,
        string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine")
    {
        return new RestoreMetadata(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            CleanupPlanSchemaVersion: "0.2",
            RestorePlanId: "abcd",
            OriginalPath: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
            QuarantinePath: Path.Combine(quarantineRoot, "items", "abcd-cache.tmp"),
            RestoreMetadataPath: Path.Combine(quarantineRoot, "restore", "abcd.restore.json"),
            RiskLevel: RiskLevel.LowRisk,
            PlanAction: CleanupPlanAction.ReviewForQuarantine,
            Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
            Warnings: ["Quarantine preview only; no file operation has been executed."],
            RequiresManualConfirmation: true,
            Redacted: redacted);
    }
}
