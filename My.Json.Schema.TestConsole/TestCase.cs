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

            string description = obj.GetValue("description").Value<string>();
            JToken data = obj.GetValue("data");
            bool valid = obj.GetValue("valid").Value<bool>();

            TestCase testCase = new TestCase
            {
                Description = description,
                Data = data,
                Valid = valid
            };
            return testCase;
        }
    }
}
