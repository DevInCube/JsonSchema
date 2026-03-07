using System;
namespace My.Json.Schema;

public class ValidationEventArgs : EventArgs
{
    public ValidationEventArgs(ValidationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        Error = error;
        Message = error.Message;
    }

    public ValidationError Error { get; }

    public string Message { get; }

}
