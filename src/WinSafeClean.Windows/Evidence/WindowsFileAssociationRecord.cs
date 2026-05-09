namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsFileAssociationRecord(
    string Scope,
    string Extension,
    string? ProgId,
    string Verb,
    string Command,
    string RegistryPath);
