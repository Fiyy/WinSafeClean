using System.Xml.Linq;

namespace WinSafeClean.CleanerRules;

public static class CleanerMlParser
{
    private static readonly Dictionary<string, CleanerCandidateKind> SupportedSearches =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["file"] = CleanerCandidateKind.File,
            ["glob"] = CleanerCandidateKind.Glob,
            ["walk.files"] = CleanerCandidateKind.WalkFiles,
            ["walk.all"] = CleanerCandidateKind.WalkAll,
            ["walk.top"] = CleanerCandidateKind.WalkTop
        };

    public static CleanerMlRuleSet Parse(string xml, CleanerMlParseOptions? options = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        options ??= CleanerMlParseOptions.Default;
        var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
        var cleanerElements = GetCleanerElements(document.Root);
        var cleaners = cleanerElements
            .Where(cleaner => AppliesToTargetOperatingSystem(cleaner, options.TargetOperatingSystem))
            .Select(cleaner => ParseCleaner(cleaner, options))
            .Where(cleaner => cleaner is not null)
            .Cast<CleanerRule>()
            .ToArray();

        return new CleanerMlRuleSet(cleaners);
    }

    private static IEnumerable<XElement> GetCleanerElements(XElement? root)
    {
        if (root is null)
        {
            return [];
        }

        if (root.Name.LocalName == "cleaner")
        {
            return [root];
        }

        return root
            .Elements()
            .Where(element => element.Name.LocalName == "cleaner");
    }

    private static CleanerRule? ParseCleaner(XElement cleanerElement, CleanerMlParseOptions options)
    {
        var id = ReadAttribute(cleanerElement, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        var optionsList = cleanerElement
            .Elements()
            .Where(element => element.Name.LocalName == "option")
            .Select(option => ParseOption(option, options))
            .Where(option => option is not null)
            .Cast<CleanerOption>()
            .ToArray();

        return new CleanerRule(
            Id: id,
            Label: ReadChildText(cleanerElement, "label") ?? id,
            Description: ReadChildText(cleanerElement, "description"),
            RunningBlockers: ParseRunningBlockers(cleanerElement, options).ToArray(),
            Options: optionsList);
    }

    private static CleanerOption? ParseOption(XElement optionElement, CleanerMlParseOptions options)
    {
        var id = ReadAttribute(optionElement, "id");
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return new CleanerOption(
            Id: id,
            Label: ReadChildText(optionElement, "label") ?? id,
            Description: ReadChildText(optionElement, "description"),
            Candidates: ParseCandidates(optionElement, options).ToArray());
    }

    private static IEnumerable<CleanerCandidate> ParseCandidates(XElement optionElement, CleanerMlParseOptions options)
    {
        foreach (var actionElement in optionElement.Elements().Where(element => element.Name.LocalName == "action"))
        {
            if (!AppliesToTargetOperatingSystem(actionElement, options.TargetOperatingSystem))
            {
                continue;
            }

            var command = ReadAttribute(actionElement, "command");
            if (!string.Equals(command, "delete", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var search = ReadAttribute(actionElement, "search") ?? "file";
            var path = ReadAttribute(actionElement, "path");
            if (string.IsNullOrWhiteSpace(path) || !SupportedSearches.TryGetValue(search, out var kind))
            {
                continue;
            }

            yield return new CleanerCandidate(
                Kind: kind,
                PathPattern: path,
                Command: "delete",
                Type: ReadAttribute(actionElement, "type"),
                Regex: ReadAttribute(actionElement, "regex"),
                WholeRegex: ReadAttribute(actionElement, "wholeregex"));
        }
    }

    private static IEnumerable<CleanerRunningBlocker> ParseRunningBlockers(
        XElement cleanerElement,
        CleanerMlParseOptions options)
    {
        foreach (var runningElement in cleanerElement.Elements().Where(element => element.Name.LocalName == "running"))
        {
            if (!AppliesToTargetOperatingSystem(runningElement, options.TargetOperatingSystem))
            {
                continue;
            }

            var type = ReadAttribute(runningElement, "type");
            var value = runningElement.Value.Trim();
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            yield return new CleanerRunningBlocker(
                Type: type,
                Value: value,
                SameUser: bool.TryParse(ReadAttribute(runningElement, "same_user"), out var sameUser) && sameUser);
        }
    }

    private static bool AppliesToTargetOperatingSystem(XElement element, string targetOperatingSystem)
    {
        var os = ReadAttribute(element, "os");
        if (string.IsNullOrWhiteSpace(os))
        {
            return true;
        }

        return os
            .Split([',', ';', ' '], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Any(value => value.Equals(targetOperatingSystem, StringComparison.OrdinalIgnoreCase));
    }

    private static string? ReadChildText(XElement element, string localName)
    {
        return element
            .Elements()
            .FirstOrDefault(child => child.Name.LocalName == localName)
            ?.Value
            .Trim();
    }

    private static string? ReadAttribute(XElement element, string localName)
    {
        return element
            .Attributes()
            .FirstOrDefault(attribute => attribute.Name.LocalName == localName)
            ?.Value
            .Trim();
    }
}
