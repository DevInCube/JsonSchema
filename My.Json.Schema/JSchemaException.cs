using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using My.Json.Schema.Utilities;

namespace My.Json.Schema
{
    public class JSchemaException : Exception
    {

        public JSchemaException() { }

        public JSchemaException(string p) : base(p) { }

        public static string FormatMessage(string Message, string Path, IJsonLineInfo LineInfo)
        {
            StringBuilder bld = new StringBuilder();

            bld.Append(Message);
            if (Path != null)
                bld.Append(" Path: '{0}' ".FormatWith(Path));
            if (LineInfo != null && LineInfo.HasLineInfo())
                bld.Append(" Line {0} Position {1} ".FormatWith(LineInfo.LineNumber, LineInfo.LinePosition));

            return bld.ToString();
        }
    }
}
