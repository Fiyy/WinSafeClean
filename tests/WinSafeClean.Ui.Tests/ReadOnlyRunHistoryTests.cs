using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class ReadOnlyRunHistoryTests
{
    [Fact]
    public void Add_ShouldKeepNewestRunsFirstWithoutDeduplicatingRepeatedTargets()
    {
        var older = new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero);
        var newer = older.AddMinutes(5);
        var entries = new[]
        {
            CreateEntry(ReadOnlyRunHistoryKind.Scan, older, @"C:\Users\Alice\Downloads", @"C:\Reports\scan-1.json")
        };

        var updated = ReadOnlyRunHistory.Add(
            entries,
            CreateEntry(ReadOnlyRunHistoryKind.Scan, newer, @"C:\Users\Alice\Downloads", @"C:\Reports\scan-2.json"),
            maxEntries: 10);

        Assert.Equal(2, updated.Count);
        Assert.Equal(@"C:\Reports\scan-2.json", updated[0].OutputPath);
        Assert.Equal(@"C:\Reports\scan-1.json", updated[1].OutputPath);
    }

    [Fact]
    public void Add_ShouldEnforceMaximumEntryCount()
    {
        var timestamp = new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero);
        var entries = Enumerable.Range(0, 5)
            .Select(index => CreateEntry(
                ReadOnlyRunHistoryKind.Plan,
                timestamp.AddMinutes(index),
                $@"C:\Targets\{index}",
                $@"C:\Reports\plan-{index}.json"))
            .ToList();

        var updated = ReadOnlyRunHistory.Add(
            entries,
            CreateEntry(ReadOnlyRunHistoryKind.Plan, timestamp.AddHours(1), @"C:\Targets\new", @"C:\Reports\plan-new.json"),
            maxEntries: 3);

        Assert.Equal(3, updated.Count);
        Assert.Equal(@"C:\Reports\plan-new.json", updated[0].OutputPath);
        Assert.DoesNotContain(updated, entry => entry.OutputPath == @"C:\Reports\plan-0.json");
    }

    [Fact]
    public void Entry_ShouldExposeUserFacingLabels()
    {
        var entry = CreateEntry(
            ReadOnlyRunHistoryKind.SafetyCheck,
            new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero),
            @"C:\Reports\plan.json",
            @"C:\Reports\check.json",
            exitCode: 2);

        Assert.Equal("Safety Check", entry.KindLabel);
        Assert.Equal("Failed", entry.StatusLabel);
        Assert.False(entry.CanOpenInUi);
        Assert.Contains("Safety Check", entry.DisplayText, StringComparison.Ordinal);
        Assert.Contains("Failed", entry.DisplayText, StringComparison.Ordinal);
    }

    [Fact]
    public void Store_ShouldRoundTripEntriesWithoutCommandOutput()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "run-history.json");
        var store = new ReadOnlyRunHistoryStore(path, maxEntries: 10);
        var timestamp = new DateTimeOffset(2026, 5, 15, 8, 0, 0, TimeSpan.Zero);

        store.Add(CreateEntry(ReadOnlyRunHistoryKind.Scan, timestamp, @"C:\Users\Alice\Downloads", @"C:\Reports\scan.json"));

        var loaded = store.Load();
        Assert.Single(loaded);
        Assert.Equal(ReadOnlyRunHistoryKind.Scan, loaded[0].Kind);
        Assert.Equal(@"C:\Users\Alice\Downloads", loaded[0].TargetPath);
        Assert.Equal(@"C:\Reports\scan.json", loaded[0].OutputPath);
        Assert.Equal("json", loaded[0].Format);
        Assert.DoesNotContain("stdout", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("stderr", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Store_ShouldReturnEmptyForMissingOrCorruptFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "run-history.json");
        var store = new ReadOnlyRunHistoryStore(path, maxEntries: 10);

        Assert.Empty(store.Load());

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{not-json");

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Store_ClearShouldRemoveEntries()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "run-history.json");
        var store = new ReadOnlyRunHistoryStore(path, maxEntries: 10);

        store.Add(CreateEntry(ReadOnlyRunHistoryKind.Scan, DateTimeOffset.UtcNow, @"C:\Temp", @"C:\Reports\scan.json"));
        store.Clear();

        Assert.Empty(store.Load());
    }

    private static ReadOnlyRunHistoryEntry CreateEntry(
        ReadOnlyRunHistoryKind kind,
        DateTimeOffset completedAt,
        string targetPath,
        string outputPath,
        int exitCode = 0)
    {
        return new ReadOnlyRunHistoryEntry(
            Kind: kind,
            TargetPath: targetPath,
            OutputPath: outputPath,
            Format: "json",
            ExitCode: exitCode,
            StartedAt: completedAt.AddSeconds(-2),
            CompletedAt: completedAt);
    }
}
