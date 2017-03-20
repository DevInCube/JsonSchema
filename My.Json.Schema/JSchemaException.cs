using Newtonsoft.Json;
using System;
using System.Text;
using My.Json.Schema.Utilities;

namespace My.Json.Schema
{
    public class JSchemaException : Exception
    {

        public JSchemaException() { }

        public JSchemaException(string p) : base(p) { }

        public static string FormatMessage(string message, string path, IJsonLineInfo lineInfo)
        {
            StringBuilder bld = new StringBuilder();

            bld.Append(message);
            if (path != null)
                bld.Append(" Path: '{0}' ".FormatWith(path));
            if (lineInfo != null && lineInfo.HasLineInfo())
                bld.Append(" Line {0} Position {1} ".FormatWith(lineInfo.LineNumber, lineInfo.LinePosition));

            return bld.ToString();
        }
    }
}
