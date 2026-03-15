using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.Json.Schema.TestConsole;

internal sealed class TestContext
{
    public TestPackage Package { get; init; }

    public string Description { get; init; }

    public JObject Schema { get; init; }

    public List<TestCase> Cases { get; init; } = [];

    internal static TestContext Create(TestPackage package, JObject testObject)
    {
        ArgumentNullException.ThrowIfNull(testObject);

        TestContext context = new()
        {
            Package = package,
            Description = testObject.GetValue("description").Value<string>(),
            Schema = (JObject)testObject.GetValue("schema"),
        };
        var cases = ((JArray)testObject.GetValue("tests"))
            .Children<JObject>()
            .Select((x, i) => TestCase.Create(context, x, i));
        context.Cases.AddRange(cases);
        return context;
    }
}

