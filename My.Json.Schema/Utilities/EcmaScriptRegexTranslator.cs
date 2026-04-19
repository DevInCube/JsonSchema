using System;
using System.Text;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities;

/// <summary>
/// Translates ECMAScript regex patterns into .NET-compatible regex patterns.
/// Handles surrogate pairs and ECMA 262 whitespace classes that .NET's
/// RegexOptions.ECMAScript doesn't fully support.
/// </summary>
public sealed class EcmaScriptRegexTranslator
{
    // ECMA 262 WhiteSpace + LineTerminator + Unicode Space_Separator (Zs).
    // .NET's ECMAScript \s/\S doesn't cover the full set (e.g. U+00A0,
    // U+FEFF, U+2028, U+2029, U+2003).
    private const string EcmaWhitespaceClass =
        @"\t\n\v\f\r \u00A0\u1680\u2000-\u200A\u2028\u2029\u202F\u205F\u3000\uFEFF";

    private readonly string _pattern;
    private readonly StringBuilder _sb;
    private int _pos;
    private bool _inCharClass;

    private EcmaScriptRegexTranslator(string pattern)
    {
        _pattern = pattern;
        _sb = new StringBuilder(pattern.Length * 2);
    }

    public static Regex Translate(string pattern, RegexOptions options = RegexOptions.None)
    {
        ArgumentNullException.ThrowIfNull(pattern);

        var translator = new EcmaScriptRegexTranslator(pattern);
        string translated = translator.Run();
        return new Regex(translated, options | RegexOptions.ECMAScript);
    }

    private string Run()
    {
        while (_pos < _pattern.Length)
        {
            if (TrySurrogatePair())
            {
                continue;
            }

            if (TryEscapeSequence())
            {
                continue;
            }

            TrackCharClassBoundary();
            _sb.Append(_pattern[_pos]);
            _pos++;
        }

        return _sb.ToString();
    }

    /// <summary>
    /// Wraps a surrogate pair in a non-capturing group so quantifiers
    /// apply to the whole codepoint, not just the low surrogate.
    /// </summary>
    private bool TrySurrogatePair()
    {
        if (!char.IsHighSurrogate(_pattern[_pos]) ||
            _pos + 1 >= _pattern.Length ||
            !char.IsLowSurrogate(_pattern[_pos + 1]))
        {
            return false;
        }

        _sb.Append("(?:")
           .Append(_pattern[_pos])
           .Append(_pattern[_pos + 1])
           .Append(')');

        _pos += 2;
        return true;
    }

    /// <summary>
    /// Translates \s / \S into explicit ECMA 262 whitespace classes.
    /// Passes all other escapes through verbatim.
    /// </summary>
    private bool TryEscapeSequence()
    {
        if (_pattern[_pos] != '\\' || _pos + 1 >= _pattern.Length)
            return false;

        char next = _pattern[_pos + 1];

        switch (next)
        {
            case 's':
                AppendWhitespaceClass(negated: false);
                break;
            case 'S':
                AppendWhitespaceClass(negated: true);
                break;
            default:
                // Emit both chars verbatim (e.g. \\ stays \\) so we don't
                // misinterpret a following 's'/'S' on the next iteration.
                _sb.Append('\\').Append(next);
                break;
        }

        _pos += 2;
        return true;
    }

    private void AppendWhitespaceClass(bool negated)
    {
        if (_inCharClass)
        {
            if (negated)
            {
                // Inside a class, .NET can't easily express class subtraction
                // under ECMAScript mode — fall back to the default \S.
                _sb.Append(@"\S");
            }
            else
            {
                _sb.Append(EcmaWhitespaceClass);
            }
        }
        else
        {
            _sb.Append(negated ? "[^" : "[")
               .Append(EcmaWhitespaceClass)
               .Append(']');
        }
    }

    private void TrackCharClassBoundary()
    {
        switch (_pattern[_pos])
        {
            case '[' when !_inCharClass:
                _inCharClass = true;
                break;
            case ']' when _inCharClass:
                _inCharClass = false;
                break;
        }
    }
}
