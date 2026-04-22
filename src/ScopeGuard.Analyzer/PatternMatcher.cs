namespace ScopeGuard.Analyzer;

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text.RegularExpressions;

internal static class PatternMatcher
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static bool IsMatch(string callerFullName, string pattern)
    {
        var regex = RegexCache.GetOrAdd(pattern, BuildRegex);
        return regex.IsMatch(callerFullName);
    }

    private static Regex BuildRegex(string pattern)
    {
        // Split on ** first to preserve it while escaping the rest
        var doubleStarParts = pattern.Split(new[] { "**" }, StringSplitOptions.None);

        var escaped = string.Join(".*", doubleStarParts.Select(part =>
        {
            // Within each part, split on * to handle single-segment wildcards
            var singleStarParts = part.Split('*');
            return string.Join("[^.]+", singleStarParts.Select(Regex.Escape));
        }));

        return new Regex($"^{escaped}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
