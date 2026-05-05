using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Windows.Evidence;

public sealed class ServiceEvidenceProvider : IFileEvidenceProvider
{
    private readonly IWindowsServiceSource serviceSource;

    public ServiceEvidenceProvider()
        : this(CreateDefaultServiceSource())
    {
    }

    public ServiceEvidenceProvider(IWindowsServiceSource serviceSource)
    {
        ArgumentNullException.ThrowIfNull(serviceSource);

        this.serviceSource = serviceSource;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var service in serviceSource.GetServices())
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(service.ImagePath))
            {
                continue;
            }

            var serviceExecutablePath = ServiceImagePathParser.TryGetExecutablePath(service.ImagePath);
            if (serviceExecutablePath is null
                || !serviceExecutablePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            evidence.Add(new EvidenceRecord(
                Type: EvidenceType.ServiceReference,
                Source: FormatSource(service),
                Confidence: 0.95,
                Message: $"Service ImagePath references this file: {service.ImagePath}"));
        }

        return evidence;
    }

    private static string FormatSource(WindowsServiceRecord service)
    {
        return string.IsNullOrWhiteSpace(service.DisplayName)
            ? service.Name
            : $"{service.Name} ({service.DisplayName})";
    }

    private static IWindowsServiceSource CreateDefaultServiceSource()
    {
        return OperatingSystem.IsWindows()
            ? new RegistryWindowsServiceSource()
            : EmptyWindowsServiceSource.Instance;
    }

    private sealed class EmptyWindowsServiceSource : IWindowsServiceSource
    {
        public static readonly EmptyWindowsServiceSource Instance = new();

        private EmptyWindowsServiceSource()
        {
        }

        public IReadOnlyList<WindowsServiceRecord> GetServices()
        {
            return [];
        }
    }
}
