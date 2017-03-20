using System;
using System.Collections.Generic;
using System.Text;
using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace My.Json.Schema
{
    public class ValidationError
    {

        private readonly string _message;
        private readonly string _path;
        private IList<ValidationError> _childErrors;

        public string Path { get { return _path; } }
        public string Message { get { return _message; } }
        public IJsonLineInfo LineInfo { get; internal set; }
        public IList<ValidationError> ChildErrors
        {
            get { return _childErrors ?? (_childErrors = new List<ValidationError>()); }
            internal set { _childErrors = value; }
        }

        public ValidationError(string message)
        {
            this._message = message;
        }

        public ValidationError(string message, JToken data)
        {
            this._message = message;
            string path = (data == null || String.IsNullOrWhiteSpace(data.Path)) ? null : data.Path;
            if(data != null)
                LineInfo = data;
            this._path = path;
        }

        internal string CreateFullMessage()
        {
            StringBuilder bld = new StringBuilder();

            bld.Append(Message);
            if (Path != null)
                bld.Append(" Path: '{0}' ".FormatWith(Path));
            if (LineInfo != null && LineInfo.HasLineInfo())
                bld.Append(" Line {0} Position {1} ".FormatWith(LineInfo.LineNumber, LineInfo.LinePosition));
            bld.AppendLine();
            foreach (var child in ChildErrors)
                bld.AppendLine(child.CreateFullMessage());

            return bld.ToString();
        }

    }
}
