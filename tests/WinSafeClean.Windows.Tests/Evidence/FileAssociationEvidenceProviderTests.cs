using WinSafeClean.Core.Reporting;
using WinSafeClean.Windows.Evidence;

namespace WinSafeClean.Windows.Tests.Evidence;

public sealed class FileAssociationEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnFileAssociationEvidenceWhenCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        var appPath = sandbox.WriteFile(@"Example\app.exe");
        var provider = new FileAssociationEvidenceProvider(new StubWindowsFileAssociationSource(
        [
            new WindowsFileAssociationRecord(
                Scope: "HKCU",
                Extension: ".example",
                ProgId: "example.file",
                Verb: "open",
                Command: $"\"{appPath}\" \"%1\"",
                RegistryPath: @"Software\Classes\example.file\shell\open\command")
        ]));

        var evidence = provider.CollectEvidence(appPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.FileAssociationReference, item.Type);
        Assert.Contains(".example", item.Source);
        Assert.Equal(0.85, item.Confidence);
        Assert.Contains("file association", item.Message);
        Assert.Contains(appPath, item.Message);
    }

    [Fact]
    public void ShouldReturnFileAssociationEvidenceWhenEnvironmentVariableCommandMatches()
    {
        using var sandbox = TemporarySandbox.Create();
        using var environmentVariable = TemporaryEnvironmentVariable.Set("WINSAFECLEAN_ASSOC_ROOT", sandbox.RootPath);
        var appPath = sandbox.WriteFile(@"Example\app.exe");
        var provider = new FileAssociationEvidenceProvider(new StubWindowsFileAssociationSource(
        [
            new WindowsFileAssociationRecord(
                Scope: "HKLM",
                Extension: ".example",
                ProgId: null,
                Verb: "edit",
                Command: @"%WINSAFECLEAN_ASSOC_ROOT%\Example\app.exe ""%1""",
                RegistryPath: @"Software\Classes\.example\shell\edit\command")
        ]));

        var evidence = provider.CollectEvidence(appPath);

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnEmptyEvidenceWhenCommandDoesNotMatch()
    {
        using var sandbox = TemporarySandbox.Create();
        var targetPath = sandbox.WriteFile(@"Example\app.exe");
        var otherPath = sandbox.WriteFile(@"Other\other.exe");
        var provider = new FileAssociationEvidenceProvider(new StubWindowsFileAssociationSource(
        [
            new WindowsFileAssociationRecord(
                Scope: "HKCU",
                Extension: ".other",
                ProgId: "other.file",
                Verb: "open",
                Command: $"\"{otherPath}\" \"%1\"",
                RegistryPath: @"Software\Classes\other.file\shell\open\command")
        ]));

        var evidence = provider.CollectEvidence(targetPath);

        Assert.Empty(evidence);
    }

    private sealed class StubWindowsFileAssociationSource(IReadOnlyList<WindowsFileAssociationRecord> records)
        : IWindowsFileAssociationSource
    {
        public IReadOnlyList<WindowsFileAssociationRecord> GetFileAssociations()
        {
            return records;
        }
    }

    private sealed class TemporarySandbox : IDisposable
    {
        private TemporarySandbox(string rootPath)
        {
            RootPath = rootPath;
        }

        public string RootPath { get; }

        public static TemporarySandbox Create()
        {
            var rootPath = Path.Combine(Path.GetTempPath(), "WinSafeClean.Windows.FileAssociation.Tests", Path.GetRandomFileName());
            Directory.CreateDirectory(rootPath);
            return new TemporarySandbox(rootPath);
        }

        public string WriteFile(string relativePath)
        {
            var path = Path.Combine(RootPath, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, string.Empty);
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

    private sealed class TemporaryEnvironmentVariable : IDisposable
    {
        private readonly string name;
        private readonly string? previousValue;

        private TemporaryEnvironmentVariable(string name, string value)
        {
            this.name = name;
            previousValue = Environment.GetEnvironmentVariable(name);
            Environment.SetEnvironmentVariable(name, value);
        }

        public static TemporaryEnvironmentVariable Set(string name, string value)
        {
            return new TemporaryEnvironmentVariable(name, value);
        }

        public void Dispose()
        {
            Environment.SetEnvironmentVariable(name, previousValue);
        }
    }
}
