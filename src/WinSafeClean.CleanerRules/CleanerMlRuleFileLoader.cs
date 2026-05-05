namespace WinSafeClean.CleanerRules;

public static class CleanerMlRuleFileLoader
{
    public static CleanerMlRuleSet LoadFile(
        string path,
        CleanerMlParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var xml = File.ReadAllText(path);
        cancellationToken.ThrowIfCancellationRequested();

        return CleanerMlParser.Parse(xml, options);
    }

    public static CleanerMlRuleSet LoadDirectory(
        string path,
        CleanerMlParseOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();

        var cleaners = new List<CleanerRule>();
        foreach (var filePath in Directory.EnumerateFiles(path, "*.xml").OrderBy(item => item, StringComparer.OrdinalIgnoreCase))
        {
            cancellationToken.ThrowIfCancellationRequested();
            cleaners.AddRange(LoadFile(filePath, options, cancellationToken).Cleaners);
        }

        return new CleanerMlRuleSet(cleaners);
    }
}
