using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using My.Json.Schema;

namespace My.Json.Schema.TestConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            string remoteHost = "http://localhost:1234";
            string remoteDirectory = "Resources/remotes";
            JSchemaResolver resolver = new JSchemaTestRemoteResolver(remoteHost, remoteDirectory);

            string testsDraft4Dir = "Resources/tests/draft4";
            var draft4Tests = LoadTests(testsDraft4Dir);

            int successCount = 0;
            int failedCount = 0;
            int exceptionCount = 0;
            foreach (TestPackage testPack in draft4Tests)
            {
                Console.WriteLine(testPack.Name + ":" + Environment.NewLine);
                foreach (TestContext test in testPack.Tests)
                {
                    Console.WriteLine("Test: " + test.Description);
                    foreach (TestCase testCase in test.Cases)
                    {
                        Console.Write("\tCase: " + testCase.Description + " ");
                        JSchema schema;
                        try
                        {
                            schema = JSchema.Parse(test.Schema.ToString(), resolver);
                        }
                        catch(Exception e)
                        {
                            exceptionCount++;
                            Console.WriteLine("\t\tException: " + e.Message);
                            continue;
                        }

                        bool result = testCase.Data.IsValid(schema);
                       
                        bool success = (result == testCase.Valid);
                        Console.WriteLine("\t\tStatus: " + (success ? "ok" : "FAILED"));
                        if (success)
                            successCount++;
                        else
                            failedCount++;
                    }
                }
                Console.WriteLine("---------------------------");
            }
            Console.WriteLine("===========================");
            Console.WriteLine("SUCCESS: \t" + successCount);
            Console.WriteLine("FAILED: \t" + failedCount);
            Console.WriteLine("EXCEPTIONS: \t" + exceptionCount);
            Console.ReadKey(true);
        }

        static List<TestPackage> LoadTests(string testsDirPath)
        {
            DirectoryInfo testsDir = new DirectoryInfo(testsDirPath);
            FileInfo[] testFiles = testsDir.GetFiles();
            List<TestPackage> allTests = new List<TestPackage>();
            foreach (var testFile in testFiles)
            {
                List<TestContext> tests = new List<TestContext>();
                string content = testFile.OpenText().ReadToEnd();
                JArray testArray = JArray.Parse(content);
                foreach (JToken item in testArray)
                {
                    if (item.Type != JTokenType.Object) throw new Exception("invalid test");
                    JObject testObject = item as JObject;
                    TestContext test = TestContext.Create(testObject);
                    tests.Add(test);
                }
                TestPackage pack = new TestPackage();
                pack.Name = testFile.Name;
                pack.Tests = tests;
                allTests.Add(pack);
            }
            return allTests;
        }
    }
}
