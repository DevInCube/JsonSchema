using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema.TestConsole
{
    class TestContext
    {

        public string Description { get; private set; }
        public JObject Schema { get; private set; }
        public IList<TestCase> Cases { get; private set; }

        internal static TestContext Create(JObject testObject)
        {
            if (testObject == null) throw new ArgumentNullException("JObject");
            
            string description = testObject.GetValue("description").Value<string>();            
            JObject schema = testObject.GetValue("schema") as JObject;
            JArray tests = testObject.GetValue("tests") as JArray;
            List<TestCase> cases = new List<TestCase>();
            foreach (JObject testCase in tests.Children<JObject>())
                cases.Add(TestCase.Create(testCase));

            TestContext context = new TestContext();
            context.Description = description;
            context.Schema = schema;
            context.Cases = cases;
            return context;
        }
    }
    
}
