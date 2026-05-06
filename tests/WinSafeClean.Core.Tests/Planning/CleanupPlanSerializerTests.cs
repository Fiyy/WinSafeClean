using System.Text.Json;
using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Planning;

public sealed class CleanupPlanSerializerTests
{
    [Fact]
    public void ShouldSerializeCleanupPlanJsonWithReadableEnumValues()
    {
        var plan = CreatePlan();

        var json = CleanupPlanJsonSerializer.Serialize(plan);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;
        Assert.Equal("0.2", root.GetProperty("schemaVersion").GetString());
        Assert.Equal("2026-05-06T01:02:03+00:00", root.GetProperty("createdAt").GetString());
        Assert.Equal(@"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine", root.GetProperty("quarantineRoot").GetString());
        var item = root.GetProperty("items")[0];
        Assert.Equal(@"C:\Temp\cache.tmp", item.GetProperty("path").GetString());
        Assert.Equal("ReviewForQuarantine", item.GetProperty("action").GetString());
        Assert.Equal("LowRisk", item.GetProperty("riskLevel").GetString());
        var preview = item.GetProperty("quarantinePreview");
        Assert.Equal(@"C:\Temp\cache.tmp", preview.GetProperty("originalPath").GetString());
        Assert.True(preview.GetProperty("requiresManualConfirmation").GetBoolean());
    }

    [Fact]
    public void ShouldRenderCleanupPlanMarkdown()
    {
        var plan = CreatePlan();

        var markdown = CleanupPlanMarkdownSerializer.Serialize(plan);

        Assert.Contains("# WinSafeClean Cleanup Plan", markdown);
        Assert.Contains(@"C:\Temp\cache.tmp", markdown);
        Assert.Contains("`ReviewForQuarantine`", markdown);
        Assert.Contains("## Quarantine Preview", markdown);
        Assert.Contains("restore.json", markdown);
        Assert.Contains("Known cleanup rule", markdown);
    }

    [Fact]
    public void ShouldEscapeMarkdownTableSeparatorsAndBackticksInPlanPath()
    {
        var plan = new CleanupPlan(
            SchemaVersion: "0.1",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Temp\cache`name|part.tmp",
                    Action: CleanupPlanAction.ReportOnly,
                    RiskLevel: RiskLevel.Unknown,
                    Reasons: ["Report only."])
            ]);

        var markdown = CleanupPlanMarkdownSerializer.Serialize(plan);

        Assert.Contains(@"cache\`name\|part.tmp", markdown);
    }

    [Fact]
    public void ShouldSanitizeControlCharactersInPlanReasons()
    {
        var plan = new CleanupPlan(
            SchemaVersion: "0.1",
            CreatedAt: DateTimeOffset.UnixEpoch,
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Temp\cache.tmp",
                    Action: CleanupPlanAction.ReportOnly,
                    RiskLevel: RiskLevel.Unknown,
                    Reasons: ["Line one\r\nLine two\t\u0001"])
            ]);

        var markdown = CleanupPlanMarkdownSerializer.Serialize(plan);

        Assert.Contains(@"Line one\r\nLine two\t\u0001", markdown);
    }

    private static CleanupPlan CreatePlan()
    {
        return new CleanupPlan(
            SchemaVersion: "0.2",
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            QuarantineRoot: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine",
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Temp\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."],
                    QuarantinePreview: new QuarantinePreview(
                        OriginalPath: @"C:\Temp\cache.tmp",
                        ProposedQuarantinePath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\items\abcd-cache.tmp",
                        RestoreMetadataPath: @"C:\Users\Alice\AppData\Local\WinSafeClean\Quarantine\restore\abcd.restore.json",
                        RestorePlanId: "abcd",
                        RequiresManualConfirmation: true,
                        Warnings: ["Quarantine preview only; no file operation has been executed."]))
            ]);
    }
}
