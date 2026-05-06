namespace WinSafeClean.Core.Quarantine;

public enum QuarantineOperationType
{
    PlanGenerated = 0,
    QuarantineStarted,
    QuarantineCompleted,
    QuarantineFailed,
    RestoreStarted,
    RestoreCompleted,
    RestoreFailed
}
