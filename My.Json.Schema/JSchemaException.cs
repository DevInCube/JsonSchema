using My.Json.Schema.Utilities;
using Newtonsoft.Json;
using System;
using System.Text;

namespace My.Json.Schema;

public class JSchemaException : Exception
{
    public JSchemaException() { }

    public JSchemaException(string message)
        : base(message)
    {
    }

    public JSchemaException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public JSchemaException(string message, string path, IJsonLineInfo lineInfo)
        : this(FormatMessage(message, path, lineInfo))
    {
    }

    private static string FormatMessage(string message, string path, IJsonLineInfo lineInfo)
    {
        StringBuilder bld = new();

        bld.Append(message);
        if (path != null)
        {
            bld.Append(" Path: '{0}' ".FormatWith(path));
        }

        if (lineInfo != null && lineInfo.HasLineInfo())
        {
            bld.Append(" Line {0} Position {1} ".FormatWith(lineInfo.LineNumber, lineInfo.LinePosition));
        }

        return bld.ToString();
    }
}
