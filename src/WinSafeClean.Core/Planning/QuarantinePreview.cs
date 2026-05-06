namespace WinSafeClean.Core.Planning;

public sealed record QuarantinePreview(
    string OriginalPath,
    string ProposedQuarantinePath,
    string RestoreMetadataPath,
    string RestorePlanId,
    bool RequiresManualConfirmation,
    IReadOnlyList<string> Warnings);
