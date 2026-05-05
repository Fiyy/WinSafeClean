using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Core.Evidence;

public sealed class CompositeFileEvidenceProvider : IFileEvidenceProvider
{
    private readonly IReadOnlyList<IFileEvidenceProvider> providers;

    public CompositeFileEvidenceProvider(IEnumerable<IFileEvidenceProvider> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);

        this.providers = providers.ToList();
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var evidence = new List<EvidenceRecord>();

        foreach (var provider in providers)
        {
            try
            {
                evidence.AddRange(provider.CollectEvidence(path));
            }
            catch (Exception ex)
            {
                evidence.Add(CreateFailureEvidence(provider, ex));
            }
        }

        return evidence;
    }

    private static EvidenceRecord CreateFailureEvidence(IFileEvidenceProvider provider, Exception exception)
    {
        return new EvidenceRecord(
            Type: EvidenceType.CollectionFailure,
            Source: provider.GetType().Name,
            Confidence: 1.0,
            Message: $"Evidence provider failed with {exception.GetType().Name}.");
    }
}
