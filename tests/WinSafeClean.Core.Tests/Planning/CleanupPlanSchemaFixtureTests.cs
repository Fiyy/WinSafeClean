using WinSafeClean.Core.Planning;
using WinSafeClean.Core.Risk;

namespace WinSafeClean.Core.Tests.Planning;

public sealed class CleanupPlanSchemaFixtureTests
{
    [Fact]
    public void ShouldMatchVersion01JsonFixture()
    {
        var plan = new CleanupPlan(
            SchemaVersion: "0.1",
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            Items:
            [
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\AppData\Local\Example\cache.tmp",
                    Action: CleanupPlanAction.ReviewForQuarantine,
                    RiskLevel: RiskLevel.LowRisk,
                    Reasons: ["Known cleanup rule matched; manual review required before quarantine."]),
                new CleanupPlanItem(
                    Path: @"C:\Users\Alice\Downloads\unknown.bin",
                    Action: CleanupPlanAction.ReportOnly,
                    RiskLevel: RiskLevel.Unknown,
                    Reasons: ["Insufficient evidence for a cleanup action; report only."]),
                new CleanupPlanItem(
                    Path: @"C:\Windows\Installer\example.msi",
                    Action: CleanupPlanAction.Keep,
                    RiskLevel: RiskLevel.Blocked,
                    Reasons: ["Blocked risk level must be kept.", "Windows Installer cache"])
            ]);

        var json = CleanupPlanJsonSerializer.Serialize(plan);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Planning", "fixtures", "cleanup-plan-v0.1.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
    }
}
