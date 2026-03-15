using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;

namespace My.Json.Schema.TestConsole;

internal sealed class TestPackage
{
    public string Name { get; init; }

    public List<TestContext> Tests { get; init; } = [];

    public static TestPackage Create(string testFileName, JArray testArray)
    {
        TestPackage package = new()
        {
            Name = testFileName,
        };

        package.Tests.AddRange(CreateTests(package, testArray));
        return package;
    }

    private static IEnumerable<TestContext> CreateTests(TestPackage package, JArray testArray)
    {
        foreach (JToken item in testArray)
        {
            if (item.Type != JTokenType.Object)
            {
                throw new InvalidDataException("invalid test");
            }

            yield return TestContext.Create(package, (JObject)item);
        }
    }
}
