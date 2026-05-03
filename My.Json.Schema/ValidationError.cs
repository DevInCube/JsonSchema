using My.Json.Schema.Utilities;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Text;

namespace My.Json.Schema;

public class ValidationError
{
    public string Path { get; }

    public string Message { get; }

    public IList<ValidationError> ChildErrors { get => field ??= []; init; }

    public ValidationError(string message)
    {
        Message = message;
    }

    public ValidationError(string message, JsonNode data)
        : this(message)
    {
        Path = !string.IsNullOrWhiteSpace(data?.GetPath()) ? data.GetPath() : null;
    }

    internal string CreateFullMessage()
    {
        StringBuilder bld = new();

        bld.Append(Message);
        if (Path != null)
        {
            bld.Append(" Path: '{0}' ".FormatWith(Path));
        }

        bld.AppendLine();
        foreach (var child in ChildErrors)
        {
            bld.AppendLine(child.CreateFullMessage());
        }

        return bld.ToString();
    }
}
