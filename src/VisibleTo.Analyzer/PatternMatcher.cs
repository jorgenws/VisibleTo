namespace VisibleTo.Analyzer;

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
        var doubleStarParts = pattern.Split(new[] { "**" }, StringSplitOptions.None);

        var escapedParts = doubleStarParts.Select(part =>
        {
            var singleStarParts = part.Split('*');
            return string.Join("[^.]+", singleStarParts.Select(Regex.Escape));
        }).ToArray();

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < escapedParts.Length - 1; i++)
        {
            var part = escapedParts[i];
            // When ** trails a literal dot (e.g. "Foo.**"), make the dot optional
            // so "Foo" itself matches alongside "Foo.Bar", "Foo.Bar.Baz", etc.
            bool trailingDoubleStar = i == escapedParts.Length - 2 && escapedParts[i + 1] == "";
            if (trailingDoubleStar && part.EndsWith(@"\."))
            {
                sb.Append(part, 0, part.Length - 2); // strip trailing \.
                sb.Append(@"(\..*)?" );               // optional .anything
            }
            else
            {
                sb.Append(part);
                sb.Append(".*");
            }
        }
        sb.Append(escapedParts[escapedParts.Length - 1]);

        return new Regex($"^{sb}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
