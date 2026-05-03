using My.Json.Schema.Utilities;
using System;
using System.Text;
using System.Text.Json.Nodes;

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

    public JSchemaException(string message, JsonNode jNode)
        : this(FormatMessage(message, jNode?.GetPath() ?? string.Empty))
    {
    }

    private static string FormatMessage(string message, string path)
    {
        StringBuilder bld = new();

        bld.Append(message);
        if (path != null)
        {
            bld.Append(" Path: '{0}' ".FormatWith(path));
        }

        return bld.ToString();
    }
}
