using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.Core.Tests.Evidence;

public sealed class CompositeFileEvidenceProviderTests
{
    [Fact]
    public void ShouldCollectEvidenceFromAllProviders()
    {
        var provider = new CompositeFileEvidenceProvider(
        [
            new StubEvidenceProvider(
            [
                new EvidenceRecord(EvidenceType.ServiceReference, "ServiceA", 0.9, "First")
            ]),
            new StubEvidenceProvider(
            [
                new EvidenceRecord(EvidenceType.ScheduledTaskReference, "TaskA", 0.8, "Second")
            ])
        ]);

        var evidence = provider.CollectEvidence(@"C:\Tools\app.exe");

        Assert.Collection(
            evidence,
            first => Assert.Equal(EvidenceType.ServiceReference, first.Type),
            second => Assert.Equal(EvidenceType.ScheduledTaskReference, second.Type));
    }

    [Fact]
    public void ShouldReturnCollectionFailureEvidenceWhenProviderThrows()
    {
        var provider = new CompositeFileEvidenceProvider(
        [
            new ThrowingEvidenceProvider()
        ]);

        var evidence = provider.CollectEvidence(@"C:\Tools\app.exe");

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.CollectionFailure, item.Type);
        Assert.Equal(nameof(ThrowingEvidenceProvider), item.Source);
        Assert.Contains("InvalidOperationException", item.Message);
    }

    private sealed class StubEvidenceProvider(IReadOnlyList<EvidenceRecord> evidence) : IFileEvidenceProvider
    {
        public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
        {
            return evidence;
        }
    }

    private sealed class ThrowingEvidenceProvider : IFileEvidenceProvider
    {
        public IReadOnlyList<EvidenceRecord> CollectEvidence(string path)
        {
            throw new InvalidOperationException("adapter failed");
        }
    }
}
