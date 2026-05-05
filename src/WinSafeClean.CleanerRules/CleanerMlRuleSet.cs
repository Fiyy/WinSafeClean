namespace WinSafeClean.CleanerRules;

public sealed record CleanerMlRuleSet(IReadOnlyList<CleanerRule> Cleaners);
