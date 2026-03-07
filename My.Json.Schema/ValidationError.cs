using System;
using System.Collections.Generic;
using System.Text;
using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace My.Json.Schema;

public class ValidationError
{
    public string Path { get; }
    public string Message { get; }
    public IJsonLineInfo LineInfo { get; internal set; }
    public IList<ValidationError> ChildErrors { get => field ??= []; internal set; }

    public ValidationError(string message)
    {
        Message = message;
    }

    public ValidationError(string message, JToken data)
    {
        Message = message;
        string path = (data == null || String.IsNullOrWhiteSpace(data.Path)) ? null : data.Path;
        if(data != null)
        {
            LineInfo = data;
        }

        Path = path;
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
