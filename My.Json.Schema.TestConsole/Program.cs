using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace My.Json.Schema.TestConsole
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            string remoteHost = "http://localhost:1234";
            string testSuiteDirectory = "Resources";
            string remoteDirectory = Path.Combine(testSuiteDirectory, @"remotes");
            JSchemaResolver resolver = new JSchemaTestRemoteResolver(remoteHost, remoteDirectory);

            string draftVersion = "draft4";
            string testsDraftDir = Path.Combine(testSuiteDirectory, "tests", draftVersion);
            var draftTests = LoadTests(testsDraftDir);

            string testsOptionalDraftDir = Path.Combine(testsDraftDir, "optional");
            var draftOptionalTests = LoadTests(testsOptionalDraftDir);

            Console.WriteLine("MAIN TESTS ====================");
            RunTests(draftTests, resolver);
            Console.WriteLine(Environment.NewLine + "OPTIONAL TESTS ====================");
            RunTests(draftOptionalTests, resolver);
            Console.ReadKey(true);         
        }

        private static void RunTests(IEnumerable<TestPackage> draftTests, JSchemaResolver resolver)
        {
            int successCount = 0;
            int failedCount = 0;
            int exceptionCount = 0;
            foreach (TestPackage testPack in draftTests)
            {
                Console.WriteLine($"{testPack.Name}:{Environment.NewLine}");
                int testSuccessCount = 0;
                int testFailedCount = 0;
                int testExceptionCount = 0;
                foreach (TestContext test in testPack.Tests)
                {
                    int caseFailedCount = 0;
                    int caseExceptionCount = 0;
                    StringBuilder builder = new StringBuilder();
                    builder.AppendLine($"Test: {test.Description}, {test.Cases.Count} test cases");
                    foreach ((TestCase testCase, int index) in test.Cases.Select((x, i) => (x, i)))
                    {
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
                            builder.AppendLine($"\t\tException: {e.Message}");
                            continue;
                        }

                        bool result = false;
                        try
                        {
                            result = testCase.Data.IsValid(schema);
                        }
                        catch (Exception e)
                        {
                            exceptionCount++;
                            testExceptionCount++;
                            caseExceptionCount++;
                            builder.AppendLine($"\t\tException: {e.Message}");
                            continue;
                        }

                        bool success = result == testCase.Valid;

                        builder.Append($"\tCase {index + 1}: {testCase.Description} ");
                        var statusString = success ? "ok" : "FAILED";
                        builder.AppendLine($"\t\tStatus: {statusString}");
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
                    {
                        Console.WriteLine(builder.ToString());
                    }
                }

                int totalCount = testSuccessCount + testFailedCount + testExceptionCount;
                Console.WriteLine($"[{testSuccessCount}/{totalCount}]---------------------------");
            }

            Console.WriteLine("===========================");
            Console.WriteLine($"SUCCESS: \t{successCount}");
            Console.WriteLine($"FAILED: \t{failedCount}");
            Console.WriteLine($"EXCEPTIONS: \t{exceptionCount}");
        }

        private static IEnumerable<TestPackage> LoadTests(string testsDirPath)
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
                    if (item.Type != JTokenType.Object)
                    {
                        throw new Exception("invalid test");
                    }

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
