namespace WinSafeClean.Windows.Evidence;

public interface IWindowsFileSignatureSource
{
    WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default);
}
