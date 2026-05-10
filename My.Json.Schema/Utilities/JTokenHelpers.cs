using System.Text.Json.Nodes;

namespace My.Json.Schema.Utilities;

internal static class JTokenHelpers
{
    private static readonly JTokenEqualityComparer s_equalityComparer = new();

    public static JsonNode GetRootParent(this JsonNode token)
    {
        while (token.Parent != null)
        {
            token = token.Parent;
        }

        return token;
    }

    public static bool IsEqualTo(this JsonNode? a, JsonNode? b)
    {
        return s_equalityComparer.Equals(a, b);
    }
}
