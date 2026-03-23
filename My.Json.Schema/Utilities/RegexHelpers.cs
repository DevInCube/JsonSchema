using System;
using System.Text;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities;

public static class RegexHelpers
{
    public static Regex Create(string pattern)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(pattern);

        try
        {
            return CreateRegexWithSurrogatePairs(pattern);
        }
        catch
        {
            throw;
        }
    }

    private static Regex CreateRegexWithSurrogatePairs(
        string pattern,
        RegexOptions options = RegexOptions.None)
    {
        var sb = new StringBuilder(pattern.Length * 2);
        for (int i = 0; i < pattern.Length; i++)
        {
            // Surrogate pair - two UTF-16 code units, one codepoint.
            if (char.IsHighSurrogate(pattern[i]) &&
                i + 1 < pattern.Length &&
                char.IsLowSurrogate(pattern[i + 1]))
            {
                // Wrap the two actual surrogate chars in a non-capturing group
                // so quantifiers apply to the whole pair, not just the low surrogate.
                sb.Append("(?:");
                sb.Append(pattern[i]); // high surrogate char
                sb.Append(pattern[i + 1]); // low surrogate char
                sb.Append(')');
                i++; // skip low surrogate — already consumed
            }
            else
            {
                sb.Append(pattern[i]);
            }
        }

        return new Regex(sb.ToString(), options | RegexOptions.ECMAScript);
    }
}
