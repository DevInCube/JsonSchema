using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
            
            string description = testObject.GetValue("description").Value<string>();            
            JObject schema = testObject.GetValue("schema") as JObject;
            JArray tests = (JArray) testObject.GetValue("tests");
            List<TestCase> cases = new List<TestCase>();
            foreach (JObject testCase in tests.Children<JObject>())
                cases.Add(TestCase.Create(testCase));

            TestContext context = new TestContext
            {
                Description = description,
                Schema = schema,
                Cases = cases
            };
            return context;
        }
    }
    
}
