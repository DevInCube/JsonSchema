using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public class ValidationEventArgs : EventArgs
    {

        private readonly ValidationError _Error;
        private readonly string _Message;

        public ValidationEventArgs(ValidationError error)
        {
            if (error == null) throw new ArgumentNullException("error");

            this._Error = error;
            this._Message = error.Message;
        }

        public ValidationError Error { get { return _Error; } }

        public string Message { get { return _Message; } }

    }
}
