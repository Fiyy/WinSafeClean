using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Reporting;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Planning;

public sealed class CleanupPlanGeneratorTests
{
    [Fact]
    public void ShouldKeepBlockedItems()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Windows\Installer\example.msi",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence: [],
            Risk: RiskAssessment.Blocked(SuggestedAction.Keep, "Windows Installer cache")));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("Blocked", item.Reasons[0]);
    }

    [Fact]
    public void ShouldKeepItemsWithActiveReferenceEvidence()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Tools\app.exe",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.RunningProcessReference, "app (PID 1)", 1.0, "Running")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.7, SuggestedAction.ReportOnly, ["Looks temporary."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("active reference", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldKeepItemsWithPathEnvironmentReferenceEvidence()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Tools\app.exe",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.PathEnvironmentReference, "User PATH", 0.7, "Parent directory is listed in PATH"),
                new EvidenceRecord(EvidenceType.KnownCleanupRule, "CleanerML: example.cache", 0.6, "Candidate")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.7, SuggestedAction.ReportOnly, ["Looks temporary."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("active reference", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Null(item.QuarantinePreview);
    }

    [Fact]
    public void ShouldKeepItemsWithShortcutReferenceEvidence()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Tools\app.exe",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.ShortcutReference, @"C:\Users\Alice\Desktop\App.lnk", 0.8, "Shortcut target"),
                new EvidenceRecord(EvidenceType.KnownCleanupRule, "CleanerML: example.cache", 0.6, "Candidate")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.7, SuggestedAction.ReportOnly, ["Looks temporary."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("active reference", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Null(item.QuarantinePreview);
    }

    [Fact]
    public void ShouldKeepItemsWithFileAssociationReferenceEvidence()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Tools\app.exe",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.FileAssociationReference, @"HKCU\Software\Classes\.example", 0.85, "Open command"),
                new EvidenceRecord(EvidenceType.KnownCleanupRule, "CleanerML: example.cache", 0.6, "Candidate")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.7, SuggestedAction.ReportOnly, ["Looks temporary."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("active reference", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Null(item.QuarantinePreview);
    }

    [Fact]
    public void ShouldKeepItemsWithMicrosoftStorePackageEvidence()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Users\Alice\AppData\Local\Packages\Contoso.App_abc123\LocalCache\cache.tmp",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.MicrosoftStorePackage, "Microsoft Store package: Contoso.App_abc123", 0.85, "Package data root"),
                new EvidenceRecord(EvidenceType.KnownCleanupRule, "CleanerML: example.cache", 0.6, "Candidate")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.7, SuggestedAction.ReportOnly, ["Looks temporary."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("installed-application", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Null(item.QuarantinePreview);
    }

    [Fact]
    public void ShouldMarkKnownCleanupRuleLowRiskItemsAsQuarantineReviewCandidates()
    {
        const string quarantineRoot = @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine";
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.KnownCleanupRule, "CleanerML: example.cache", 0.6, "Candidate")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.8, SuggestedAction.ReportOnly, ["Known cache."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch, quarantineRoot);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.ReviewForQuarantine, item.Action);
        Assert.Contains("manual review", string.Join(" ", item.Reasons), StringComparison.OrdinalIgnoreCase);
        Assert.Equal(quarantineRoot, plan.QuarantineRoot);
        Assert.NotNull(item.QuarantinePreview);
        Assert.Equal(item.Path, item.QuarantinePreview.OriginalPath);
        Assert.StartsWith(quarantineRoot, item.QuarantinePreview.ProposedQuarantinePath, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("no file operation", string.Join(" ", item.QuarantinePreview.Warnings), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ShouldKeepItemsWhenEvidenceCollectionFailed()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.CollectionFailure, "Provider", 1.0, "Failed")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.8, SuggestedAction.ReportOnly, ["Known cache."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.Keep, item.Action);
        Assert.Contains("Evidence collection failed", item.Reasons);
    }

    [Fact]
    public void ShouldNotTreatFileSignatureAsCleanupPermission()
    {
        var report = CreateReport(new ScanReportItem(
            Path: @"C:\Users\Alice\Downloads\signed.tmp",
            ItemKind: ScanReportItemKind.File,
            SizeBytes: 1024,
            LastWriteTimeUtc: null,
            Evidence:
            [
                new EvidenceRecord(EvidenceType.FileSignature, "Authenticode", 0.6, "Signature metadata")
            ],
            Risk: new RiskAssessment(RiskLevel.LowRisk, 0.8, SuggestedAction.ReportOnly, ["Signed file."], [])));

        var plan = CleanupPlanGenerator.Generate(report, DateTimeOffset.UnixEpoch);

        var item = Assert.Single(plan.Items);
        Assert.Equal(CleanupPlanAction.ReportOnly, item.Action);
        Assert.Contains("Insufficient evidence", item.Reasons[0]);
        Assert.Null(item.QuarantinePreview);
    }

    private static ScanReport CreateReport(ScanReportItem item)
    {
        return new ScanReport(
            SchemaVersion: "1.3",
            PrivacyMode: ScanReportPrivacyMode.Full,
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items: [item]);
    }
}
