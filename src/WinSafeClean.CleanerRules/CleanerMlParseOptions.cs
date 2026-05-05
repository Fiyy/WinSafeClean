namespace WinSafeClean.CleanerRules;

public sealed record CleanerMlParseOptions(string TargetOperatingSystem = "windows")
{
    public static CleanerMlParseOptions Default { get; } = new();
}
