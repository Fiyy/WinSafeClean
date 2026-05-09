using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class MicrosoftStorePackageEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsStorePackageSource storePackageSource;

    public MicrosoftStorePackageEvidenceProvider()
        : this(CreateDefaultStorePackageSource())
    {
    }

    public MicrosoftStorePackageEvidenceProvider(IWindowsStorePackageSource storePackageSource)
    {
        ArgumentNullException.ThrowIfNull(storePackageSource);

        this.storePackageSource = storePackageSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        var evidence = new List<EvidenceRecord>();

        foreach (var package in storePackageSource.GetStorePackages())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!IsPathInsidePackageRoot(normalizedPath, package.PackageRoot))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.MicrosoftStorePackage,
                Source: $"Microsoft Store package: {package.PackageName}",
                Confidence: 0.85,
                Message: $"Path is under Microsoft Store package {package.RootKind}: {package.PackageRoot}"));
        }

        return evidence;
    }

    private static bool IsPathInsidePackageRoot(string normalizedPath, string packageRoot)
    {
        if (string.IsNullOrWhiteSpace(packageRoot))
        {
            return false;
        }

        try
        {
            var normalizedRoot = Path.GetFullPath(packageRoot)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            return normalizedPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(
                    normalizedRoot + Path.DirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase)
                || normalizedPath.StartsWith(
                    normalizedRoot + Path.AltDirectorySeparatorChar,
                    StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    private static IWindowsStorePackageSource CreateDefaultStorePackageSource()
    {
        return OperatingSystem.IsWindows()
            ? new FileSystemWindowsStorePackageSource()
            : EmptyWindowsStorePackageSource.Instance;
    }

    private sealed class EmptyWindowsStorePackageSource : IWindowsStorePackageSource
    {
        public static readonly EmptyWindowsStorePackageSource Instance = new();

        private EmptyWindowsStorePackageSource()
        {
        }

        public IReadOnlyList<WindowsStorePackageRecord> GetStorePackages()
        {
            return [];
        }
    }
}
