using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public class ValidationError
    {

        private readonly string message;
        private IList<ValidationError> _childErrors;

        public string Message { get { return message; } }
        public IList<ValidationError> ChildErrors
        {
            get
            {
                if (_childErrors == null)
                    _childErrors = new List<ValidationError>();

                return _childErrors;
            }
            internal set { _childErrors = value; }
        }

        public ValidationError(string message)
        {
            this.message = message;
        }

        internal string CreateFullMessage()
        {
            StringBuilder bld = new StringBuilder();

            bld.AppendLine(Message);
            foreach (var child in ChildErrors)
                bld.AppendLine(child.CreateFullMessage());

            return bld.ToString();
        }
    }
}
