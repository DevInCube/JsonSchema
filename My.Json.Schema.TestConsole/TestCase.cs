using Newtonsoft.Json.Linq;
using System;

namespace My.Json.Schema.TestConsole;

internal sealed class TestCase
{
    public string Description { get; init; }

    public JToken Data { get; init; }

    public bool Valid { get; init; }

    internal static TestCase Create(JObject obj)
    {
        ArgumentNullException.ThrowIfNull(obj);

        return new TestCase
        {
            Description = obj.GetValue("description").Value<string>(),
            Data = obj.GetValue("data"),
            Valid = obj.GetValue("valid").Value<bool>()
        };
    }
}
