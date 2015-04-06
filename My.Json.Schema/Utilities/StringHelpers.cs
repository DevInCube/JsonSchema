using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema.Utilities
{
    public static class StringHelpers
    {
        public static bool ContainsUnicodeCharacter(string input)
        {
            const int MaxAnsiCode = 255;

            return input.Any(c => c > MaxAnsiCode);
        }
    }
}
