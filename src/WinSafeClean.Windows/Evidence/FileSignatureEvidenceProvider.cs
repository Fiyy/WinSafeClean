using System.Security;
using System.Security.Cryptography;
using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class FileSignatureEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsFileSignatureSource signatureSource;

    public FileSignatureEvidenceProvider()
        : this(CreateDefaultSignatureSource())
    {
    }

    public FileSignatureEvidenceProvider(IWindowsFileSignatureSource signatureSource)
    {
        ArgumentNullException.ThrowIfNull(signatureSource);

        this.signatureSource = signatureSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        WindowsFileSignatureRecord? signature;
        try
        {
            signature = signatureSource.GetSignature(path, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception) when (IsExpectedSignatureReadFailure(exception))
        {
            return [];
        }

        cancellationToken.ThrowIfCancellationRequested();

        if (signature is null)
        {
            return [];
        }

        return
        [
            new EvidenceRecord(
                Type: EvidenceType.FileSignature,
                Source: "Authenticode",
                Confidence: 0.6,
                Message: FormatMessage(signature))
        ];
    }

    private static string FormatMessage(WindowsFileSignatureRecord signature)
    {
        return $"Authenticode signature present. Subject: {signature.Subject}; Issuer: {signature.Issuer}; Thumbprint: {signature.Thumbprint}";
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

    private static IWindowsFileSignatureSource CreateDefaultSignatureSource()
    {
        return OperatingSystem.IsWindows()
            ? new AuthenticodeFileSignatureSource()
            : EmptyWindowsFileSignatureSource.Instance;
    }

    private sealed class EmptyWindowsFileSignatureSource : IWindowsFileSignatureSource
    {
        public static readonly EmptyWindowsFileSignatureSource Instance = new();

        private EmptyWindowsFileSignatureSource()
        {
        }

        public WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return null;
        }
    }
}
