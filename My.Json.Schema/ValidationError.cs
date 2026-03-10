using My.Json.Schema.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;

namespace My.Json.Schema;

public class ValidationError
{
    public string Path { get; }

    public string Message { get; }

    public IJsonLineInfo LineInfo { get; }

    public IList<ValidationError> ChildErrors { get => field ??= []; init; }

    public ValidationError(string message)
    {
        Message = message;
    }

    public ValidationError(string message, JToken data)
        : this(message)
    {
        Path = !string.IsNullOrWhiteSpace(data?.Path) ? data.Path : null;
        LineInfo = data;
    }

    internal string CreateFullMessage()
    {
        StringBuilder bld = new();

        bld.Append(Message);
        if (Path != null)
        {
            bld.Append(" Path: '{0}' ".FormatWith(Path));
        }

        if (LineInfo != null && LineInfo.HasLineInfo())
        {
            bld.Append(" Line {0} Position {1} ".FormatWith(LineInfo.LineNumber, LineInfo.LinePosition));
        }

        bld.AppendLine();
        foreach (var child in ChildErrors)
        {
            bld.AppendLine(child.CreateFullMessage());
        }

        return bld.ToString();
    }
}
