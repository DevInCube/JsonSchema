using Newtonsoft.Json.Linq;
using System;

namespace My.Json.Schema.TestConsole
{
    internal class TestCase
    {

        public string Description { get; private set; }
        public JToken Data { get; private set; }
        public bool Valid { get; private set; }

        internal static TestCase Create(JObject obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            return new TestCase
            {
                Description = obj.GetValue("description").Value<string>(),
                Data = obj.GetValue("data"),
                Valid = obj.GetValue("valid").Value<bool>()
            };
        }
    }
}
