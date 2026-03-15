using System;
using System.Text;

namespace My.Json.Schema.TestConsole;

internal sealed class TestExecutionContext : IDisposable
{
    private readonly TestExecutionContext _parent;
    private readonly StringBuilder _builder = new();

    public int SuccessCount { get; private set; }

    public int FailedCount { get; private set; }

    public int ExceptionCount { get; private set; }

    public bool IsSuccess => FailedCount == 0 && ExceptionCount == 0;

    public int Total => SuccessCount + FailedCount + ExceptionCount;

    public TestExecutionContext(TestExecutionContext parent = null)
    {
        _parent = parent;
    }

    public void Dispose()
    {
        LogToParent();
    }

    public void Success()
    {
        SuccessCount += 1;
        _parent?.Success();
    }

    public void Fail()
    {
        FailedCount += 1;
        _parent?.Fail();
    }

    public void Exception(Exception ex)
    {
        ExceptionCount += 1;
        _parent?.Exception(ex);
    }

    public void Log(string message)
    {
        if (_parent == null)
        {
            Console.WriteLine(message);
            return;
        }

        _builder.AppendLine(message);
    }

    public void Log(Exception ex)
    {
        Log($"\t\tException: {ex.Message}");
    }

    public void ClearLog()
    {
        _builder.Clear();
    }

    private void LogToParent()
    {
        if (_builder.Length == 0)
        {
            return;
        }

        _parent?.Log(_builder.ToString());
    }
}