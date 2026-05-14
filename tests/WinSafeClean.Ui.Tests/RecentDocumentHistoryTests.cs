using WinSafeClean.Ui.Operations;

namespace WinSafeClean.Ui.Tests;

public sealed class RecentDocumentHistoryTests
{
    [Fact]
    public void Add_MovesExistingEntryToTop()
    {
        var older = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        var newer = new DateTimeOffset(2026, 5, 10, 9, 0, 0, TimeSpan.Zero);
        var entries = new[]
        {
            new RecentDocumentEntry(RecentDocumentKind.ScanReport, @"C:\Reports\a.json", older),
            new RecentDocumentEntry(RecentDocumentKind.CleanupPlan, @"C:\Reports\b.json", older)
        };

        var updated = RecentDocumentHistory.Add(
            entries,
            RecentDocumentKind.ScanReport,
            @"C:\Reports\a.json",
            newer,
            maxEntries: 10);

        Assert.Equal(2, updated.Count);
        Assert.Equal(RecentDocumentKind.ScanReport, updated[0].Kind);
        Assert.Equal(newer, updated[0].LastOpenedAt);
        Assert.Equal(@"C:\Reports\a.json", updated[0].Path);
    }

    [Fact]
    public void Add_DoesNotMergeDifferentKindsForSamePath()
    {
        var timestamp = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        var entries = new[]
        {
            new RecentDocumentEntry(RecentDocumentKind.ScanReport, @"C:\Reports\shared.json", timestamp)
        };

        var updated = RecentDocumentHistory.Add(
            entries,
            RecentDocumentKind.CleanupPlan,
            @"C:\Reports\shared.json",
            timestamp.AddMinutes(1),
            maxEntries: 10);

        Assert.Equal(2, updated.Count);
        Assert.Contains(updated, entry => entry.Kind == RecentDocumentKind.ScanReport);
        Assert.Contains(updated, entry => entry.Kind == RecentDocumentKind.CleanupPlan);
    }

    [Fact]
    public void Add_EnforcesMaximumEntryCount()
    {
        var timestamp = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);
        var entries = Enumerable.Range(0, 5)
            .Select(index => new RecentDocumentEntry(
                RecentDocumentKind.ScanReport,
                $@"C:\Reports\{index}.json",
                timestamp.AddMinutes(index)))
            .ToList();

        var updated = RecentDocumentHistory.Add(
            entries,
            RecentDocumentKind.ScanReport,
            @"C:\Reports\new.json",
            timestamp.AddHours(1),
            maxEntries: 3);

        Assert.Equal(3, updated.Count);
        Assert.Equal(@"C:\Reports\new.json", updated[0].Path);
        Assert.DoesNotContain(updated, entry => entry.Path == @"C:\Reports\0.json");
    }

    [Fact]
    public void Store_LoadReturnsEmptyForMissingOrCorruptFile()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "recent.json");
        var store = new RecentDocumentHistoryStore(path, maxEntries: 10);

        Assert.Empty(store.Load());

        Directory.CreateDirectory(directory);
        File.WriteAllText(path, "{not-json");

        Assert.Empty(store.Load());
    }

    [Fact]
    public void Store_AddRoundTripsEntries()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "recent.json");
        var store = new RecentDocumentHistoryStore(path, maxEntries: 10);
        var timestamp = new DateTimeOffset(2026, 5, 10, 8, 0, 0, TimeSpan.Zero);

        store.Add(RecentDocumentKind.PreflightChecklist, @"C:\Reports\preflight.json", timestamp);

        var loaded = store.Load();
        Assert.Single(loaded);
        Assert.Equal(RecentDocumentKind.PreflightChecklist, loaded[0].Kind);
        Assert.Equal(@"C:\Reports\preflight.json", loaded[0].Path);
        Assert.Equal(timestamp, loaded[0].LastOpenedAt);
        Assert.StartsWith("Safety Check - ", loaded[0].DisplayText, StringComparison.Ordinal);
    }

    [Fact]
    public void Store_ClearRemovesAllEntries()
    {
        string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string path = Path.Combine(directory, "recent.json");
        var store = new RecentDocumentHistoryStore(path, maxEntries: 10);

        store.Add(RecentDocumentKind.ScanReport, @"C:\Reports\scan.json", DateTimeOffset.UtcNow);
        store.Clear();

        Assert.Empty(store.Load());
    }
}
