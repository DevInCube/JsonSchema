using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema.TestConsole
{
    class TestCase
    {

        public string Description { get; private set; }
        public JToken Data { get; private set; }
        public bool Valid { get; private set; }

        internal static TestCase Create(JObject obj)
        {
            if (obj == null) throw new ArgumentNullException("JObject");

            string description = obj.GetValue("description").Value<string>();
            JToken data = obj.GetValue("data");
            bool valid = obj.GetValue("valid").Value<bool>();

            TestCase testCase = new TestCase();
            testCase.Description = description;
            testCase.Data = data;
            testCase.Valid = valid;
            return testCase;
        }
    }
}
