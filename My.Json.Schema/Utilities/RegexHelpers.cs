using System;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities;

public static class RegexHelpers
{
    public static Regex Create(string pattern)
    {
        ArgumentNullException.ThrowIfNullOrEmpty(pattern);

        try
        {
            return EcmaScriptRegexTranslator.Translate(pattern);
        }
        catch
        {
            throw;
        }
    }
}
