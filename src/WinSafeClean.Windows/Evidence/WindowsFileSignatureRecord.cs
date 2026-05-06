namespace WinSafeClean.Windows.Evidence;

public sealed record WindowsFileSignatureRecord(
    string Subject,
    string Issuer,
    string Thumbprint);
