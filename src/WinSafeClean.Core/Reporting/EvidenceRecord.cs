namespace WinSafeClean.Core.Reporting;

public sealed record EvidenceRecord(
    EvidenceType Type,
    string Source,
    double Confidence,
    string Message);
