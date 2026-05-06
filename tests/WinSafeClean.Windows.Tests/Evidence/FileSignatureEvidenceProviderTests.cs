using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class FileSignatureEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnFileSignatureEvidenceWhenSourceReturnsSignature()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("signed.exe");
        var provider = new FileSignatureEvidenceProvider(new StubWindowsFileSignatureSource(
            new WindowsFileSignatureRecord(
                Subject: "CN=Example Publisher",
                Issuer: "CN=Example CA",
                Thumbprint: "ABC123")));

        var evidence = provider.CollectEvidence(filePath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.FileSignature, item.Type);
        Assert.Equal("Authenticode", item.Source);
        Assert.Equal(0.6, item.Confidence);
        Assert.Contains("Authenticode signature present", item.Message);
        Assert.Contains("CN=Example Publisher", item.Message);
        Assert.Contains("CN=Example CA", item.Message);
        Assert.Contains("ABC123", item.Message);
        Assert.DoesNotContain(filePath, item.Message);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenSourceReturnsNoSignature()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("unsigned.exe");
        var provider = new FileSignatureEvidenceProvider(new StubWindowsFileSignatureSource(null));

        var evidence = provider.CollectEvidence(filePath);

        Assert.Empty(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenSourceCannotReadSignature()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("locked.exe");
        var provider = new FileSignatureEvidenceProvider(
            new ThrowingWindowsFileSignatureSource(new UnauthorizedAccessException("denied")));

        var evidence = provider.CollectEvidence(filePath);

        Assert.Empty(evidence);
    }

    [Fact]
    public void ShouldPropagateCancellationBeforeReadingSignature()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("signed.exe");
        var source = new TrackingWindowsFileSignatureSource();
        var provider = new FileSignatureEvidenceProvider(source);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        Assert.Throws<OperationCanceledException>(() => provider.CollectEvidence(filePath, cancellation.Token));
        Assert.False(source.WasCalled);
    }

    [Fact]
    public void AuthenticodeSourceShouldReturnEmptyForMissingFile()
    {
        using var sandbox = TemporarySandbox.Create();
        var source = new AuthenticodeFileSignatureSource();

        var signature = source.GetSignature(sandbox.GetPath("missing.exe"));

        Assert.Null(signature);
    }

    [Fact]
    public void AuthenticodeSourceShouldReturnEmptyForDirectory()
    {
        using var sandbox = TemporarySandbox.Create();
        var directoryPath = sandbox.CreateDirectory("folder");
        var source = new AuthenticodeFileSignatureSource();

        var signature = source.GetSignature(directoryPath);

        Assert.Null(signature);
    }

    [Fact]
    public void AuthenticodeSourceShouldReturnEmptyForUnsignedFile()
    {
        using var sandbox = TemporarySandbox.Create();
        var filePath = sandbox.WriteFile("unsigned.txt", "plain text");
        var source = new AuthenticodeFileSignatureSource();

        var signature = source.GetSignature(filePath);

        Assert.Null(signature);
    }

    private sealed class StubWindowsFileSignatureSource(WindowsFileSignatureRecord? signature)
        : IWindowsFileSignatureSource
    {
        public WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return signature;
        }
    }

    private sealed class ThrowingWindowsFileSignatureSource(Exception exception) : IWindowsFileSignatureSource
    {
        public WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default)
        {
            throw exception;
        }
    }

    private sealed class TrackingWindowsFileSignatureSource : IWindowsFileSignatureSource
    {
        public bool WasCalled { get; private set; }

        public WindowsFileSignatureRecord? GetSignature(string path, CancellationToken cancellationToken = default)
        {
            WasCalled = true;
            return null;
        }
    }

    private sealed class TemporarySandbox : IDisposable
    {
        private TemporarySandbox(string rootPath)
        {
            RootPath = rootPath;
        }

        private string RootPath { get; }

        public static TemporarySandbox Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string GetPath(string relativePath)
        {
            return Path.Combine(RootPath, relativePath);
        }

        public string CreateDirectory(string relativePath)
        {
            var path = GetPath(relativePath);
            Directory.CreateDirectory(path);
            return path;
        }

        public string WriteFile(string relativePath, string contents = "")
        {
            var path = GetPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, contents);
            return path;
        }

        public void Dispose()
        {
            if (Directory.Exists(RootPath))
            {
                Directory.Delete(RootPath, recursive: true);
            }
        }
    }
}
