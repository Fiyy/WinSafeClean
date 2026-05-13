namespace WinSafeClean.Ui.ViewModels;

public sealed record PrivacySharingAdvice(
    string PrivacyMode,
    bool NeedsCaution,
    string Message);

public static class PrivacySharingAdvisor
{
    public static PrivacySharingAdvice Create(string? privacyMode)
    {
        var normalized = string.IsNullOrWhiteSpace(privacyMode)
            ? "full"
            : privacyMode.Trim().ToLowerInvariant();

        if (normalized.Equals("redacted", StringComparison.Ordinal))
        {
            return new PrivacySharingAdvice(
                normalized,
                NeedsCaution: false,
                Message: "Redacted output uses reduced path exposure for sharing.");
        }

        return new PrivacySharingAdvice(
            "full",
            NeedsCaution: true,
            Message: "Full output includes local paths and evidence. Use redacted before sharing outside this PC.");
    }
}
