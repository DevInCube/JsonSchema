using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        var testCase = GetTestCase(draftTests, "refRemote.json", "root ref in remote ref", "object is invalid");
        RunTest(testCase, resolver);

        Console.WriteLine("MAIN TESTS ====================");
        RunTests(draftTests, resolver);
        Console.WriteLine(Environment.NewLine + "OPTIONAL TESTS ====================");
        RunTests(draftOptionalTests, resolver);
        Console.ReadKey(true);
    }

    private static TestCase GetTestCase(IEnumerable<TestPackage> packages, string packageName, string contextName, string caseName)
    {
        TestPackage package = packages
            .FirstOrDefault(x => x.Name.Equals(packageName, StringComparison.Ordinal))
            ?? throw new ArgumentException($"package {packageName} is missing", nameof(packageName));
        TestContext context = package.Tests
            .FirstOrDefault(x => x.Description.Equals(contextName, StringComparison.Ordinal))
            ?? throw new ArgumentException($"context {contextName} is missing in package {packageName}", nameof(contextName));
        TestCase test = context.Cases
            .FirstOrDefault(x => x.Description.Equals(caseName, StringComparison.Ordinal))
            ?? throw new ArgumentException($"test case {caseName} is missing in context {contextName} of package {packageName}", nameof(caseName));
        return test;
    }

    private static void RunTest(TestCase testCase, JSchemaResolver resolver)
    {
        using TestExecutionContext executionContext = new();
        executionContext.Log($"Package: {testCase.Context.Package.Name}");
        executionContext.Log($"  Test context: {testCase.Context.Description}:");
        RunTestCase(testCase, resolver, executionContext);
    }

    private static void RunTests(IEnumerable<TestPackage> draftTests, JSchemaResolver resolver)
    {
        using TestExecutionContext rootExecutionContext = new();
        foreach (TestPackage testPack in draftTests)
        {
            rootExecutionContext.Log($"{testPack.Name}:{Environment.NewLine}");
            using TestExecutionContext packExecutionContext = new(rootExecutionContext);
            foreach (TestContext testContext in testPack.Tests)
            {
                using TestExecutionContext testExecutionContext = new(packExecutionContext);
                testExecutionContext.Log($"Test context: {testContext.Description}, {testContext.Cases.Count} test cases");
                foreach (TestCase testCase in testContext.Cases)
                {
                    using TestExecutionContext testCaseExecutionContext = new(testExecutionContext);
                    RunTestCase(testCase, resolver, testCaseExecutionContext);
                }

                if (testExecutionContext.FailedCount == 0 && testExecutionContext.ExceptionCount == 0)
                {
                    testExecutionContext.ClearLog();
                }
            }

            packExecutionContext.Log($"[{packExecutionContext.SuccessCount}/{packExecutionContext.Total}]---------------------------");
        }

        rootExecutionContext.Log("===========================");
        rootExecutionContext.Log($"SUCCESS: \t{rootExecutionContext.SuccessCount}");
        rootExecutionContext.Log($"FAILED: \t{rootExecutionContext.FailedCount}");
        rootExecutionContext.Log($"EXCEPTIONS: \t{rootExecutionContext.ExceptionCount}");
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static void RunTestCase(TestCase testCase, JSchemaResolver resolver, TestExecutionContext context)
    {
        context.Log($"\tCase {testCase.Index + 1}: {testCase.Description}");

        JSchema schema;
        try
        {
            schema = JSchema.Parse(testCase.Context.Schema.ToString(), resolver);
        }
        catch (Exception e)
        {
            context.Exception(e);
            context.Log("\t\tParsing schema");
            context.Log(e);
            return;
        }

        bool result = false;
        try
        {
            result = testCase.Data.IsValid(schema);
        }
        catch (Exception e)
        {
            context.Exception(e);
            context.Log("\t\tValidating data");
            context.Log(e);
            return;
        }

        bool success = result == testCase.Valid;
        var statusString = success ? "ok" : "FAILED";
        context.Log($"\t\tStatus: {statusString}");

        if (!success)
        {
            context.Fail();
            return;
        }

        context.Success();
        context.ClearLog();
    }

    private static IEnumerable<TestPackage> LoadTests(string testsDirPath)
    {
        DirectoryInfo testsDir = new(testsDirPath);
        FileInfo[] testFiles = testsDir.GetFiles();
        foreach (var testFile in testFiles)
        {
            string content = testFile.OpenText().ReadToEnd();
            JArray testArray = JArray.Parse(content);
            yield return TestPackage.Create(testFile.Name, testArray);
        }
    }
}
