namespace WinSafeClean.Core.Risk;

public sealed record RiskAssessment(
    RiskLevel Level,
    double Confidence,
    SuggestedAction SuggestedAction,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> Blockers)
{
    public static RiskAssessment Blocked(SuggestedAction suggestedAction, string blocker)
    {
        return new RiskAssessment(
            RiskLevel.Blocked,
            1.0,
            suggestedAction,
            ["Matched a protected Windows path rule."],
            [blocker]);
    }

    public static RiskAssessment Unknown(string reason)
    {
        return new RiskAssessment(
            RiskLevel.Unknown,
            0.0,
            SuggestedAction.ReportOnly,
            [reason],
            []);
    }
}
