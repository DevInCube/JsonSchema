using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace My.Json.Schema;

public static class SchemaExtensions
{
    public static bool IsValid(this JToken data, JSchema schema, out IList<ValidationError> errors, JSchemaValidationReader validationReader = null)
    {
        IList<ValidationError> messages = [];

        data.Validate(schema, (_, args) => messages.Add(args.Error), validationReader);

        errors = messages;
        return errors.Count == 0;
    }

    public static bool IsValid(this JToken data, JSchema schema, out IList<string> errors, JSchemaValidationReader validationReader = null)
    {
        IList<string> messages = [];

        data.Validate(schema, (_, args) => messages.Add(args.Error.CreateFullMessage()), validationReader);

        errors = messages;
        return errors.Count == 0;
    }

    public static bool IsValid(this JToken data, JSchema schema, JSchemaValidationReader validationReader = null)
    {
        bool valid = true;
        data.Validate(schema, (_, __) => valid = false, validationReader);
        return valid;
    }

    public static void Validate(this JToken data, JSchema schema, EventHandler<ValidationEventArgs> handler, JSchemaValidationReader parentReader = null)
    {
        JSchemaValidationReader validationReader = new(parentReader);
        validationReader.ErrorHandled += handler;
        validationReader.Validate(data, schema);
    }
}
