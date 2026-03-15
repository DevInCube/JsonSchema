using Newtonsoft.Json.Linq;
using System;

namespace My.Json.Schema.TestConsole;

internal sealed class TestCase
{
    public TestContext Context { get; init; }

    public int Index { get; init; }

    public string Description { get; init; }

    public JToken Data { get; init; }

    public bool Valid { get; init; }

    internal static TestCase Create(TestContext context, JObject obj, int index)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return new TestCase
        {
            Context = context,
            Index = index,
            Description = obj.GetValue("description").Value<string>(),
            Data = obj.GetValue("data"),
            Valid = obj.GetValue("valid").Value<bool>()
        };
    }
}
