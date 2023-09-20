using System;
using System.Net;
using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities
{
    internal static class StringHelpers
    {
        public static string FormatWith(this string pattern, params object[] values)
        {
            return String.Format(pattern, values);
        }

        public static bool IsValidHostName(string value)
        {
            return Regex.IsMatch(
                value,
                @"^(?=.{1,255}$)[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?(?:\.[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?)*\.?$",
                RegexOptions.CultureInvariant);
        }

        public static bool IsValidIPv4(string value)
        {
            return
                IPAddress.TryParse(value, out IPAddress ipAddress) &&
                ipAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork;
        }

        public static bool IsValidRegex(string value)
        {
            if (String.IsNullOrEmpty(value)) return false;

            try
            {
                _ = new Regex(value);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
