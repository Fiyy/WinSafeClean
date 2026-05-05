using WinSafeClean.Core.Reporting;

namespace WinSafeClean.CleanerRules.Tests;

public sealed class CleanerRuleEvidenceProviderTests
{
    [Fact]
    public void ShouldReturnKnownCleanupRuleEvidenceWhenFileCandidateMatches()
    {
        var targetPath = Path.GetFullPath(@"C:\Users\Alice\AppData\Local\Example\cache.db");
        var provider = new CleanerRuleEvidenceProvider(CreateRuleSet(
            new CleanerCandidate(
                Kind: CleanerCandidateKind.File,
                PathPattern: targetPath,
                Command: "delete",
                Type: null,
                Regex: null,
                WholeRegex: null)));

        var evidence = provider.CollectEvidence(targetPath);

        var item = Assert.Single(evidence);
        Assert.Equal(EvidenceType.KnownCleanupRule, item.Type);
        Assert.Equal("CleanerML: example.cache", item.Source);
        Assert.Equal(0.6, item.Confidence);
        Assert.Contains("File", item.Message);
        Assert.Contains(targetPath, item.Message);
    }

    [Fact]
    public void ShouldReturnKnownCleanupRuleEvidenceWhenGlobCandidateMatches()
    {
        var provider = new CleanerRuleEvidenceProvider(CreateRuleSet(
            new CleanerCandidate(
                Kind: CleanerCandidateKind.Glob,
                PathPattern: @"C:\Users\Alice\AppData\Local\Example\Cache\*.tmp",
                Command: "delete",
                Type: null,
                Regex: null,
                WholeRegex: null)));

        var evidence = provider.CollectEvidence(@"C:\Users\Alice\AppData\Local\Example\Cache\abc.tmp");

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldReturnKnownCleanupRuleEvidenceWhenWalkCandidateContainsPath()
    {
        var provider = new CleanerRuleEvidenceProvider(CreateRuleSet(
            new CleanerCandidate(
                Kind: CleanerCandidateKind.WalkFiles,
                PathPattern: @"C:\Users\Alice\AppData\Local\Example\Cache",
                Command: "delete",
                Type: null,
                Regex: null,
                WholeRegex: null)));

        var evidence = provider.CollectEvidence(@"C:\Users\Alice\AppData\Local\Example\Cache\nested\abc.tmp");

        Assert.Single(evidence);
    }

    [Fact]
    public void ShouldIncludeRunningBlockerInEvidenceMessage()
    {
        var targetPath = Path.GetFullPath(@"C:\Users\Alice\AppData\Local\Example\cache.db");
        var provider = new CleanerRuleEvidenceProvider(CreateRuleSet(
            new CleanerCandidate(
                Kind: CleanerCandidateKind.File,
                PathPattern: targetPath,
                Command: "delete",
                Type: null,
                Regex: null,
                WholeRegex: null),
            new CleanerRunningBlocker("exe", "example.exe", SameUser: true)));

        var evidence = provider.CollectEvidence(targetPath);

        var item = Assert.Single(evidence);
        Assert.Contains("example.exe", item.Message);
    }

    private static CleanerMlRuleSet CreateRuleSet(
        CleanerCandidate candidate,
        CleanerRunningBlocker? runningBlocker = null)
    {
        var blockers = runningBlocker is null
            ? Array.Empty<CleanerRunningBlocker>()
            : [runningBlocker];

        return new CleanerMlRuleSet(
        [
            new CleanerRule(
                Id: "example",
                Label: "Example",
                Description: null,
                RunningBlockers: blockers,
                Options:
                [
                    new CleanerOption(
                        Id: "cache",
                        Label: "Cache",
                        Description: null,
                        Candidates: [candidate])
                ])
        ]);
    }
}
