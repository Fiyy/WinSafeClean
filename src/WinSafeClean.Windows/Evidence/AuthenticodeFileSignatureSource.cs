using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace WinSafeClean.Windows.Evidence;

public sealed class AuthenticodeFileSignatureSource : IWindowsFileSignatureSource
{
    public WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var normalizedPath = Path.GetFullPath(path);
            if (!File.Exists(normalizedPath))
            {
                return null;
            }

            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(normalizedPath));
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(certificate.Subject))
            {
                return null;
            }

            return new WindowsFileSignatureRecord(
                Subject: certificate.Subject,
                Issuer: certificate.Issuer,
                Thumbprint: certificate.Thumbprint ?? string.Empty);
        }
        catch (Exception exception) when (IsExpectedSignatureReadFailure(exception))
        {
            return null;
        }
    }

    private static bool IsExpectedSignatureReadFailure(Exception exception)
    {
        return exception is FileNotFoundException
            or DirectoryNotFoundException
            or UnauthorizedAccessException
            or IOException
            or ArgumentException
            or NotSupportedException
            or PlatformNotSupportedException
            or PathTooLongException
            or SecurityException
            or CryptographicException;
    }
}
