using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace My.Json.Schema.Utilities
{
    public static class DateTimeHelpers
    {

        private static readonly string DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'"; 

        public static string ToJsonString(this DateTime datetime)
        {
            return datetime.ToString(DateTimeFormat);
        }

        public static bool IsValidDateTimeFormat(string value)
        {
            DateTime temp;
            return DateTime.TryParseExact(
                value,
                DateTimeFormat, 
                CultureInfo.InvariantCulture, 
                DateTimeStyles.AssumeUniversal, 
                out temp);     
        }
    }
}
