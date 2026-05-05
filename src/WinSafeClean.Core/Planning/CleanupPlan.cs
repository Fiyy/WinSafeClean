namespace WinSafeClean.Core.Planning;

public sealed record CleanupPlan(
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    IReadOnlyList<CleanupPlanItem> Items);
