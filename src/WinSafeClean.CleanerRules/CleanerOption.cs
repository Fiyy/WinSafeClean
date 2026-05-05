namespace WinSafeClean.CleanerRules;

public sealed record CleanerOption(
    string Id,
    string Label,
    string? Description,
    IReadOnlyList<CleanerCandidate> Candidates);
