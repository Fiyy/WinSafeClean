using WinSafeClean.Core.Quarantine;

namespace WinSafeClean.Core.Tests.Quarantine;

public sealed class QuarantinePreflightChecklistSerializerTests
{
    [Fact]
    public void ShouldSerializeChecklistWithReadableEnumValues()
    {
        var checklist = CreateChecklist();

        var json = QuarantinePreflightChecklistJsonSerializer.Serialize(checklist);

        Assert.Contains(@"""schemaVersion"": ""1.0""", json);
        Assert.Contains(@"""status"": ""Passed""", json);
    }

    [Fact]
    public void ShouldMatchVersion10JsonFixture()
    {
        var checklist = CreateChecklist();

        var json = QuarantinePreflightChecklistJsonSerializer.Serialize(checklist);
        var expected = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Quarantine", "fixtures", "preflight-checklist-v1.0.json"));

        Assert.Equal(NormalizeLineEndings(expected), NormalizeLineEndings(json));
    }

    private static QuarantinePreflightChecklist CreateChecklist()
    {
        return new QuarantinePreflightChecklist(
            SchemaVersion: "1.0",
            CreatedAt: new DateTimeOffset(2026, 5, 6, 1, 2, 3, TimeSpan.Zero),
            IsExecutable: true,
            Checks:
            [
                new QuarantinePreflightCheck(
                    Code: "ManualConfirmation",
                    Status: QuarantinePreflightCheckStatus.Passed,
                    Message: "Manual confirmation was provided.")
            ]);
    }

    private static string NormalizeLineEndings(string text)
    {
        return text
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .TrimEnd('\n');
    }
}
