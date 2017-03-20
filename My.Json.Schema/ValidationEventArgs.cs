using System;
namespace My.Json.Schema
{
    public class ValidationEventArgs : EventArgs
    {

        private readonly ValidationError _error;
        private readonly string _message;

        public ValidationEventArgs(ValidationError error)
        {
            if (error == null) throw new ArgumentNullException("error");

            this._error = error;
            this._message = error.Message;
        }

        public ValidationError Error { get { return _error; } }

        public string Message { get { return _message; } }

    }
}
