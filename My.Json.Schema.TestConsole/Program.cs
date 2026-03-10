using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace My.Json.Schema.TestConsole;

[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1305:Specify IFormatProvider")]
internal static class Program
{
    private static void Main(string[] args)
    {
        string remoteHost = "http://localhost:1234";
        string testSuiteDirectoryName = "Resources";
        string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string testSuiteDirectory = Path.Combine(applicationDirectory, testSuiteDirectoryName);
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

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
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
                StringBuilder builder = new();
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
        DirectoryInfo testsDir = new(testsDirPath);
        FileInfo[] testFiles = testsDir.GetFiles();
        foreach (var testFile in testFiles)
        {
            List<TestContext> tests = [];
            string content = testFile.OpenText().ReadToEnd();
            JArray testArray = JArray.Parse(content);
            foreach (JToken item in testArray)
            {
                if (item.Type != JTokenType.Object)
                {
                    throw new InvalidDataException("invalid test");
                }

                tests.Add(TestContext.Create((JObject)item));
            }

            yield return new TestPackage
            {
                Name = testFile.Name,
                Tests = tests,
            };
        }
    }
}
