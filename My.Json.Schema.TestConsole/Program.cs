using System.Text.Json.Nodes;
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
        string applicationDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string testSuiteDirectory = FindTestSuiteDirectory(applicationDirectory);
        // Always use the Resources/remotes copy — it includes draft-04/schema and draft-06/schema
        // which some tests reference and which the submodule remotes/ does not provide.
        string remoteDirectory = Path.Combine(applicationDirectory, "Resources", "remotes");
        JSchemaResolver resolver = new JSchemaTestRemoteResolver(remoteHost, remoteDirectory);

        RunDraft("draft4", SchemaVersion.Draft4, testSuiteDirectory, resolver);
        Console.WriteLine();
        RunDraft("draft6", SchemaVersion.Draft6, testSuiteDirectory, resolver);
        Console.WriteLine();
        RunDraft("draft7", SchemaVersion.Draft7, testSuiteDirectory, resolver);
        Console.WriteLine();
        RunDraft("draft2019-09", SchemaVersion.Draft2019_09, testSuiteDirectory, resolver);
    }

    private static string FindTestSuiteDirectory(string startDir)
    {
        // Walk up from the binary directory to find the git submodule.
        // Falls back to the Resources copy bundled alongside the binary.
        var dir = new DirectoryInfo(startDir);
        while (dir != null)
        {
            string candidate = Path.Combine(dir.FullName, "JSON-Schema-Test-Suite");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        return Path.Combine(startDir, "Resources");
    }

    private static void RunDraft(string draftName, SchemaVersion version, string testSuiteDirectory, JSchemaResolver resolver)
    {
        string testsDraftDir = Path.Combine(testSuiteDirectory, "tests", draftName);
        var draftTests = LoadTests(testsDraftDir);

        string testsOptionalDraftDir = Path.Combine(testsDraftDir, "optional");
        var draftOptionalTests = LoadTests(testsOptionalDraftDir);

        Console.WriteLine($"=== {draftName.ToUpperInvariant()} MAIN TESTS ===");
        using var mainResults = RunTests(draftTests, resolver, version);
        Console.WriteLine($"{Environment.NewLine}=== {draftName.ToUpperInvariant()} OPTIONAL TESTS ===");
        using var optionalResults = RunTests(draftOptionalTests, resolver, version);

        Console.WriteLine();
        Console.WriteLine(
            $"SUITE_RESULT draft={draftName} " +
            $"mandatory_success={mainResults.SuccessCount} " +
            $"mandatory_failed={mainResults.FailedCount} " +
            $"mandatory_exceptions={mainResults.ExceptionCount} " +
            $"optional_success={optionalResults.SuccessCount} " +
            $"optional_failed={optionalResults.FailedCount} " +
            $"optional_exceptions={optionalResults.ExceptionCount}");
    }

    private static TestExecutionContext RunTests(IEnumerable<TestPackage> draftTests, JSchemaResolver resolver, SchemaVersion version)
    {
        var rootExecutionContext = new TestExecutionContext();
        foreach (TestPackage testPack in draftTests)
        {
            rootExecutionContext.Log($"{testPack.Name}:{Environment.NewLine}");
            using TestExecutionContext packExecutionContext = new(rootExecutionContext);
            foreach (TestContext testContext in testPack.Tests)
            {
                using TestExecutionContext testExecutionContext = new(packExecutionContext);
                testExecutionContext.Log($"Test context: '{testContext.Description}', {testContext.Cases.Count} test cases");
                foreach (TestCase testCase in testContext.Cases)
                {
                    using TestExecutionContext testCaseExecutionContext = new(testExecutionContext);
                    RunTestCase(testCase, resolver, testCaseExecutionContext, version);
                }

                if (testExecutionContext.IsSuccess)
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
        return rootExecutionContext;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    private static void RunTestCase(TestCase testCase, JSchemaResolver resolver, TestExecutionContext context, SchemaVersion version)
    {
        context.Log($"\tCase {testCase.Index + 1}: '{testCase.Description}'");

        JSchema schema;
        try
        {
            schema = JSchema.Parse(testCase.Context.Schema?.ToString() ?? "{}", resolver, version);
        }
        catch (Exception e)
        {
            context.Exception(e);
            context.Log("\t\tParsing schema");
            context.Log(e);
            return;
        }

        bool result;
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
        if (!testsDir.Exists)
        {
            yield break;
        }

        FileInfo[] testFiles = testsDir.GetFiles();
        foreach (var testFile in testFiles)
        {
            string content = testFile.OpenText().ReadToEnd();
            JsonArray testArray = (JsonArray)JsonArray.Parse(content)!;
            yield return TestPackage.Create(testFile.Name, testArray);
        }
    }
}
