using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.Json.Schema.TestConsole;

internal sealed class TestContext
{
    public TestPackage Package { get; init; } = null!;

    public string Description { get; init; } = string.Empty;

    public JsonNode? Schema { get; init; }

    public List<TestCase> Cases { get; init; } = [];

    internal static TestContext Create(TestPackage package, JsonObject testObject)
    {
        ArgumentNullException.ThrowIfNull(testObject);

        TestContext context = new()
        {
            Package = package,
            Description = testObject.TryGetPropertyValue("description", out JsonNode? desc) ? desc?.GetValue<string>() ?? string.Empty : string.Empty,
            Schema = testObject.TryGetPropertyValue("schema", out JsonNode? schema) ? schema : null,
        };
        var cases = (testObject.TryGetPropertyValue("tests", out JsonNode? arr) ? (JsonArray?)arr : null)
            ?.Select((x, i) => TestCase.Create(context, x!.AsObject(), i))
            ?? [];
        context.Cases.AddRange(cases);
        return context;
    }
}

