using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace My.Json.Schema.TestConsole
{
    internal class TestContext
    {

        public string Description { get; private set; }
        public JObject Schema { get; private set; }
        public IList<TestCase> Cases { get; private set; }

        internal static TestContext Create(JObject testObject)
        {
            if (testObject == null) throw new ArgumentNullException("testObject");

            return new TestContext
            {
                Description = testObject.GetValue("description").Value<string>(),
                Schema = (JObject) testObject.GetValue("schema"),
                Cases = ((JArray) testObject.GetValue("tests"))
                    .Children<JObject>()
                    .Select(TestCase.Create)
                    .ToList()
            };
        }
    }
    
}
