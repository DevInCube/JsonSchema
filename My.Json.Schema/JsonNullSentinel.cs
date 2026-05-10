using System.Text.Json.Nodes;

namespace My.Json.Schema;

// https://github.com/microsoft/OpenAPI.NET/blob/main/src/Microsoft.OpenApi/JsonNullSentinel.cs
internal static class JsonNullSentinel
{
    // A sentinel string value that cannot appear in real schema data.
    // Used to represent JSON null in schema properties so that C# null can mean "not set".
    private const string JsonNullSentinelValue = "my-json-schema-null-8F3A2B1C-4D5E-6F7A-8B9C-0D1E2F3A4B5C";
    private static readonly JsonValue s_jsonNullSentinel = JsonValue.Create(JsonNullSentinelValue)!;

    /// <summary>
    /// Sentinel representing a JSON null value. C# null on a <see cref="JsonNode"/> property
    /// means "not set"; this sentinel means "explicitly set to JSON null".
    /// </summary>
    internal static JsonNode JsonNull => s_jsonNullSentinel;

    /// <summary>
    /// Returns true when <paramref name="node"/> represents JSON null — either C# null
    /// or the sentinel value (which may have been cloned into a JSON tree).
    /// </summary>
    internal static bool IsJsonNull(this JsonNode? node) =>
        node is null ||
        node == s_jsonNullSentinel ||
        (node.GetValueKind() == System.Text.Json.JsonValueKind.String &&
         JsonNode.DeepEquals(s_jsonNullSentinel, node));
}
