using System.Text.RegularExpressions;
using WinSafeClean.Core.Evidence;
using WinSafeClean.Core.Reporting;

namespace WinSafeClean.CleanerRules;

public sealed class CleanerRuleEvidenceProvider : IFileEvidenceProvider
{
    private readonly CleanerMlRuleSet ruleSet;

    public CleanerRuleEvidenceProvider(CleanerMlRuleSet ruleSet)
    {
        ArgumentNullException.ThrowIfNull(ruleSet);

        this.ruleSet = ruleSet;
    }

    public IReadOnlyList<EvidenceRecord> CollectEvidence(string path, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var normalizedPath = Path.GetFullPath(path);
        var evidence = new List<EvidenceRecord>();

        foreach (var cleaner in ruleSet.Cleaners)
        {
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var option in cleaner.Options)
            {
                cancellationToken.ThrowIfCancellationRequested();

                foreach (var candidate in option.Candidates)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Matches(normalizedPath, candidate))
                    {
                        continue;
                    }

                    evidence.Add(new EvidenceRecord(
                        Type: EvidenceType.KnownCleanupRule,
                        Source: $"CleanerML: {cleaner.Id}.{option.Id}",
                        Confidence: 0.6,
                        Message: FormatMessage(candidate, cleaner.RunningBlockers)));
                }
            }
        }

        return evidence;
    }

    private static bool Matches(string normalizedPath, CleanerCandidate candidate)
    {
        return candidate.Kind switch
        {
            CleanerCandidateKind.File => MatchesFile(normalizedPath, candidate.PathPattern),
            CleanerCandidateKind.Glob => MatchesGlob(normalizedPath, candidate.PathPattern),
            CleanerCandidateKind.WalkFiles => IsUnderDirectory(normalizedPath, candidate.PathPattern),
            CleanerCandidateKind.WalkAll => IsUnderDirectory(normalizedPath, candidate.PathPattern),
            CleanerCandidateKind.WalkTop => MatchesFile(normalizedPath, candidate.PathPattern)
                || IsUnderDirectory(normalizedPath, candidate.PathPattern),
            _ => false
        };
    }

    private static bool MatchesFile(string normalizedPath, string pathPattern)
    {
        var candidatePath = TryNormalizePath(pathPattern);
        return candidatePath is not null
            && candidatePath.Equals(normalizedPath, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesGlob(string normalizedPath, string pathPattern)
    {
        var expandedPattern = ExpandCleanerMlVariables(pathPattern).Replace('/', '\\');
        var regexPattern = "^"
            + Regex.Escape(expandedPattern)
                .Replace("\\*", ".*", StringComparison.Ordinal)
                .Replace("\\?", ".", StringComparison.Ordinal)
            + "$";

        return Regex.IsMatch(normalizedPath, regexPattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }

    private static bool IsUnderDirectory(string normalizedPath, string pathPattern)
    {
        var directoryPath = TryNormalizePath(pathPattern);
        if (directoryPath is null)
        {
            return false;
        }

        directoryPath = directoryPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return normalizedPath.StartsWith(
            directoryPath + Path.DirectorySeparatorChar,
            StringComparison.OrdinalIgnoreCase);
    }

    private static string? TryNormalizePath(string path)
    {
        try
        {
            return Path.GetFullPath(ExpandCleanerMlVariables(path).Replace('/', '\\'));
        }
        catch (ArgumentException)
        {
            return null;
        }
        catch (NotSupportedException)
        {
            return null;
        }
        catch (PathTooLongException)
        {
            return null;
        }
    }

    private static string ExpandCleanerMlVariables(string path)
    {
        var expanded = path;
        expanded = ReplaceCleanerMlVariable(expanded, "$localappdata", "LOCALAPPDATA");
        expanded = ReplaceCleanerMlVariable(expanded, "$appdata", "APPDATA");
        expanded = ReplaceCleanerMlVariable(expanded, "$temp", "TEMP");
        expanded = ReplaceCleanerMlVariable(expanded, "$USERPROFILE", "USERPROFILE");

        if (expanded.StartsWith("~", StringComparison.Ordinal))
        {
            var userProfile = Environment.GetEnvironmentVariable("USERPROFILE");
            if (!string.IsNullOrWhiteSpace(userProfile))
            {
                expanded = userProfile + expanded[1..];
            }
        }

        return Environment.ExpandEnvironmentVariables(expanded);
    }

    private static string ReplaceCleanerMlVariable(string value, string cleanerMlVariable, string environmentVariable)
    {
        var environmentValue = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(environmentValue)
            ? value
            : value.Replace(cleanerMlVariable, environmentValue, StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatMessage(
        CleanerCandidate candidate,
        IReadOnlyList<CleanerRunningBlocker> runningBlockers)
    {
        var message = $"CleanerML {candidate.Kind} candidate matched: {candidate.PathPattern}";
        if (runningBlockers.Count == 0)
        {
            return message;
        }

        var blockers = string.Join(
            ", ",
            runningBlockers.Select(blocker => $"{blocker.Type}:{blocker.Value}"));
        return $"{message}. Rule has running blockers: {blockers}.";
    }
}
