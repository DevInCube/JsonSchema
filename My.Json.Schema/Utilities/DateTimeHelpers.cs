using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities;

public static partial class DateTimeHelpers
{
    // RFC 3339: date "T" time offset, where offset is Z or ±HH:MM
    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CompileRegex();

    private static readonly Regex Rfc3339Regex = CompileRegex();

    public static string ToJsonString(this DateTime datetime)
    {
        return datetime.ToString("o", CultureInfo.InvariantCulture);
    }

    public static bool IsValidDateTimeFormat(string value)
    {
        if (!Rfc3339Regex.IsMatch(value))
        {
            return false;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

}
