namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsStorePackageRecord(
    string PackageName,
    string PackageRoot,
    string RootKind);
