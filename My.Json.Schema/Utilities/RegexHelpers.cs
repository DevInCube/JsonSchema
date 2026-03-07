using System.Text.RegularExpressions;

namespace My.Json.Schema.Utilities
{
    public static class RegexHelpers
    {
        public static Regex Create(string pattern)
        {
            try
            {
                return new Regex(pattern);
            }
            catch
            {
                throw;
            }
        }
    }
}
