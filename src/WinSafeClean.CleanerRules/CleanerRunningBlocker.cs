namespace WinSafeClean.CleanerRules;

public sealed record CleanerRunningBlocker(string Type, string Value, bool SameUser);
