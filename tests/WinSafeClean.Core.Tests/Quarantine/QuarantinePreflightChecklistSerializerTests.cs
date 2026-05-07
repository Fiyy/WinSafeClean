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

    [Fact]
    public void ShouldDeserializeChecklistWithReadableEnumValues()
    {
        var json = QuarantinePreflightChecklistJsonSerializer.Serialize(CreateChecklist());

        var checklist = QuarantinePreflightChecklistJsonSerializer.Deserialize(json);

        Assert.True(checklist.IsExecutable);
        var check = Assert.Single(checklist.Checks);
        Assert.Equal("ManualConfirmation", check.Code);
        Assert.Equal(QuarantinePreflightCheckStatus.Passed, check.Status);
    }

    [Fact]
    public void ShouldRenderChecklistMarkdown()
    {
        var checklist = CreateChecklist();

        var markdown = QuarantinePreflightChecklistMarkdownSerializer.Serialize(checklist);

        Assert.Contains("# WinSafeClean Preflight Checklist", markdown);
        Assert.Contains("ManualConfirmation", markdown);
        Assert.Contains("`Passed`", markdown);
    }

    [Fact]
    public void ShouldSanitizeControlCharactersInChecklistMarkdown()
    {
        var checklist = new QuarantinePreflightChecklist(
            SchemaVersion: "1.0",
            CreatedAt: DateTimeOffset.UnixEpoch,
            IsExecutable: false,
            Checks:
            [
                new QuarantinePreflightCheck(
                    Code: "Bad|Code",
                    Status: QuarantinePreflightCheckStatus.Failed,
                    Message: "Line one\r\nLine two\t\u0001")
            ]);

        var markdown = QuarantinePreflightChecklistMarkdownSerializer.Serialize(checklist);

        Assert.Contains("Bad\\|Code", markdown);
        Assert.Contains(@"Line one\r\nLine two\t\u0001", markdown);
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
