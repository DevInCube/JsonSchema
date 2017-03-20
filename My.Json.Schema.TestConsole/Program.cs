using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace My.Json.Schema.TestConsole
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string remoteHost = "http://localhost:1234";
            string remoteDirectory = "Resources/remotes";
            JSchemaResolver resolver = new JSchemaTestRemoteResolver(remoteHost, remoteDirectory);

            string testsDraft4Dir = "Resources/tests/draft4";
            var draft4Tests = LoadTests(testsDraft4Dir);

            string testsOptionalDraft4Dir = "Resources/tests/draft4/optional";
            var draft4OptionalTests = LoadTests(testsOptionalDraft4Dir);

            Console.WriteLine("MAIN TESTS ====================");
            RunTests(draft4Tests, resolver);
            Console.WriteLine(Environment.NewLine + "OPTIONAL TESTS ====================");
            RunTests(draft4OptionalTests, resolver);
            Console.ReadKey(true);         
        }

        private static void RunTests(IEnumerable<TestPackage> draft4Tests, JSchemaResolver resolver)
        {
            int successCount = 0;
            int failedCount = 0;
            int exceptionCount = 0;
            foreach (TestPackage testPack in draft4Tests)
            {
                Console.WriteLine(testPack.Name + ":" + Environment.NewLine);
                int testSuccessCount = 0;
                int testFailedCount = 0;
                int testExceptionCount = 0;
                foreach (TestContext test in testPack.Tests)
                {
                    if (test.Description.Equals("change resolution scope"))
                    {

                    }
                    int caseFailedCount = 0;
                    int caseExceptionCount = 0;
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine("Test: " + test.Description);
                    foreach (TestCase testCase in test.Cases)
                    {
                        if (testCase.Description.Equals("a valid date-time string"))
                        {

                        }
                        JSchema schema;
                        try
                        {
                            schema = JSchema.Parse(test.Schema.ToString(), resolver);
                        }
                        catch (Exception e)
                        {
                            exceptionCount++;
                            testExceptionCount++;
                            caseExceptionCount++;
                            builder.AppendLine("\t\tException: " + e.Message);
                            continue;
                        }

                        bool result = testCase.Data.IsValid(schema);
                        bool success = result == testCase.Valid;

                        builder.Append("\tCase: " + testCase.Description + " ");
                        builder.AppendLine("\t\tStatus: " + (success ? "ok" : "FAILED"));
                        if (success)
                        {
                            successCount++;
                            testSuccessCount++;
                        }
                        else
                        {
                            failedCount++;
                            testFailedCount++;
                            caseFailedCount++;
                        }
                    }
                    if (caseFailedCount > 0 || caseExceptionCount > 0)
                        Console.WriteLine(builder.ToString());
                }
                int totalCount = testSuccessCount + testFailedCount + testExceptionCount;
                Console.WriteLine("[{0}/{1}]---------------------------", testSuccessCount, totalCount);
            }
            Console.WriteLine("===========================");
            Console.WriteLine("SUCCESS: \t" + successCount);
            Console.WriteLine("FAILED: \t" + failedCount);
            Console.WriteLine("EXCEPTIONS: \t" + exceptionCount);
        }

        private static List<TestPackage> LoadTests(string testsDirPath)
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
                    tests.Add(TestContext.Create((JObject)item));
                }
                allTests.Add(new TestPackage
                {
                    Name = testFile.Name,
                    Tests = tests
                });
            }
            return allTests;
        }
    }
}
