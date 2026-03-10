using System;
using System.Globalization;

namespace My.Json.Schema.Utilities;

public static class DateTimeHelpers
{
    private const string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'";

    public static string ToJsonString(this DateTime datetime)
    {
        return datetime.ToString(DateTimeFormat, CultureInfo.InvariantCulture);
    }

    public static bool IsValidDateTimeFormat(string value)
    {
        return DateTime.TryParseExact(
            value,
            DateTimeFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out DateTime _);
    }
}
