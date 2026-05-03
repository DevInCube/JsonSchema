using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace My.Json.Schema.TestConsole;

internal sealed class TestPackage
{
    public string Name { get; init; }

    public List<TestContext> Tests { get; init; } = [];

    public static TestPackage Create(string testFileName, JsonArray testArray)
    {
        TestPackage package = new()
        {
            Name = testFileName,
        };

        package.Tests.AddRange(CreateTests(package, testArray));
        return package;
    }

    private static IEnumerable<TestContext> CreateTests(TestPackage package, JsonArray testArray)
    {
        foreach (JsonNode item in testArray)
        {
            if (item.GetValueKind() != JsonValueKind.Object)
            {
                throw new InvalidDataException("invalid test");
            }

            yield return TestContext.Create(package, (JsonObject)item);
        }
    }
}
