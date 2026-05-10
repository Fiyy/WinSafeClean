using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class ReadOnlyOperationOutputPathSuggesterTests
{
    [Fact]
    public void SuggestJsonPath_UsesOperationNameAndTimestamp()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var timestamp = new DateTimeOffset(2026, 5, 10, 14, 30, 12, TimeSpan.Zero);

        string path = ReadOnlyOperationOutputPathSuggester.SuggestJsonPath(directory, "scan", timestamp);

        Assert.Equal(
            Path.Combine(directory, "winsafeclean-scan-20260510-143012.json"),
            path);
    }

    [Fact]
    public void SuggestJsonPath_SanitizesOperationName()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var timestamp = new DateTimeOffset(2026, 5, 10, 14, 30, 12, TimeSpan.Zero);

        string path = ReadOnlyOperationOutputPathSuggester.SuggestJsonPath(directory, "scan / report", timestamp);

        Assert.Equal(
            Path.Combine(directory, "winsafeclean-scan-report-20260510-143012.json"),
            path);
    }

    [Fact]
    public void SuggestJsonPath_AvoidsExistingFiles()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var timestamp = new DateTimeOffset(2026, 5, 10, 14, 30, 12, TimeSpan.Zero);
        File.WriteAllText(Path.Combine(directory, "winsafeclean-plan-20260510-143012.json"), "{}");

        string path = ReadOnlyOperationOutputPathSuggester.SuggestJsonPath(directory, "plan", timestamp);

        Assert.Equal(
            Path.Combine(directory, "winsafeclean-plan-20260510-143012-01.json"),
            path);
    }
}
