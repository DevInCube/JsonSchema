using System;
namespace My.Json.Schema;

public class ValidationEventArgs : EventArgs
{
    public ValidationError Error { get; }

    public string Message { get; }

    public ValidationEventArgs(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        Message = error.Message;
    }
}
