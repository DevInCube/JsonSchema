using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities
{
    public static class StringHelpers
    {

        public static bool ContainsUnicodeCharacter(string input)
        {
            const int maxAnsiCode = 255;

            return input.Any(c => c > maxAnsiCode);
        }

        public static string FormatWith(this string pattern, params object[] values)
        {
            return String.Format(pattern, values);
        }

        internal static bool IsValidHostName(string value)
        {
            return Regex.IsMatch(
                value,
                @"^(?=.{1,255}$)[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?(?:\.[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?)*\.?$",
                RegexOptions.CultureInvariant);
        }

        internal static bool IsValidIPv4(string value)
        {
            string[] parts = value.Split('.');
            if (parts.Length != 4)
                return false;

            foreach (string part in parts)
            {
                int num;
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out num)
                    || (num < 0 || num > 255))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool IsValidRegex(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;

            try
            {
                new Regex(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
