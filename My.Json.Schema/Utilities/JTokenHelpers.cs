using Newtonsoft.Json.Linq;

namespace My.Json.Schema.Utilities;

internal static class JTokenHelpers
{
    private static readonly JTokenEqualityComparer s_equalityComparer = new();

    public static bool IsString(this JToken t)
    {
        return
            t.Type == JTokenType.Undefined
            || t.Type == JTokenType.Null
            || t.Type == JTokenType.String;
    }

    public static JToken GetRootParent(this JToken token)
    {
        while (token.Parent != null)
        {
            token = token.Parent;
        }

        return token;
    }

    public static bool IsEqualTo(this JToken a, JToken b)
    {
        return s_equalityComparer.Equals(a, b);
    }
}
