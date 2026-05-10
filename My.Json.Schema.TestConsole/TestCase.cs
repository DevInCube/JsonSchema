using System.Text.Json.Nodes;
using System;

namespace My.Json.Schema.TestConsole;

internal sealed class TestCase
{
    public TestContext Context { get; init; } = null!;

    public int Index { get; init; }

    public string Description { get; init; } = string.Empty;

    public JsonNode? Data { get; init; }

    public bool Valid { get; init; }

    internal static TestCase Create(TestContext context, JsonObject obj, int index)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return new TestCase
        {
            Context = context,
            Index = index,
            Description = obj.TryGetPropertyValue("description", out JsonNode? desc) ? desc?.GetValue<string>() ?? string.Empty : string.Empty,
            Data = obj.TryGetPropertyValue("data", out JsonNode? data) ? data : JsonValue.Create(string.Empty),
            Valid = obj.TryGetPropertyValue("valid", out JsonNode? valid) && (valid?.GetValue<bool>() ?? false),
        };
    }
}
