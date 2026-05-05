namespace WinSafeClean.CleanerRules;

public sealed record CleanerRule(
    string Id,
    string Label,
    string? Description,
    IReadOnlyList<CleanerRunningBlocker> RunningBlockers,
    IReadOnlyList<CleanerOption> Options);
