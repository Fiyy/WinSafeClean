using WinSafeClean.Core.Quarantine;

namespace WinSafeClean.Ui.ViewModels;

public sealed class PreflightChecklistOverviewViewModel
{
    private PreflightChecklistOverviewViewModel(
        string schemaVersion,
        bool isExecutable,
        int totalChecks,
        IReadOnlyList<SummaryItemViewModel> statusSummaries,
        IReadOnlyList<PreflightCheckItemViewModel> checks)
    {
        SchemaVersion = schemaVersion;
        IsExecutable = isExecutable;
        TotalChecks = totalChecks;
        StatusSummaries = statusSummaries;
        Checks = checks;
    }

    public string SchemaVersion { get; }

    public bool IsExecutable { get; }

    public int TotalChecks { get; }

    public IReadOnlyList<SummaryItemViewModel> StatusSummaries { get; }

    public IReadOnlyList<PreflightCheckItemViewModel> Checks { get; }

    public static PreflightChecklistOverviewViewModel Empty { get; } = new("n/a", false, 0, [], []);

    public static PreflightChecklistOverviewViewModel FromChecklist(QuarantinePreflightChecklist checklist)
    {
        ArgumentNullException.ThrowIfNull(checklist);

        var checks = checklist.Checks
            .Select(check => new PreflightCheckItemViewModel(
                Code: check.Code,
                Status: check.Status.ToString(),
                Message: check.Message))
            .ToList();

        return new PreflightChecklistOverviewViewModel(
            schemaVersion: checklist.SchemaVersion,
            isExecutable: checklist.IsExecutable,
            totalChecks: checks.Count,
            statusSummaries: checks
                .GroupBy(check => check.Status, StringComparer.Ordinal)
                .OrderBy(group => group.Key, StringComparer.Ordinal)
                .Select(group => new SummaryItemViewModel(group.Key, group.Count()))
                .ToList(),
            checks: checks);
    }
}
