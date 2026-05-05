namespace WinSafeClean.CleanerRules;

public sealed record CleanerCandidate(
    CleanerCandidateKind Kind,
    string PathPattern,
    string Command,
    string? Type,
    string? Regex,
    string? WholeRegex);
