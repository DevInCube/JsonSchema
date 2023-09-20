using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace My.Json.Schema
{
    public static class SchemaExtensions
    {

        public static bool IsValid(this JToken data, JSchema schema, out IList<ValidationError> errors, JSchemaValidationReader validationReader = null)
        {
            IList<ValidationError> messages = new List<ValidationError>();

            data.Validate(schema, (sender, args) => { messages.Add(args.Error); }, validationReader);

            errors = messages;
            return (errors.Count == 0);
        }

        public static bool IsValid(this JToken data, JSchema schema, out IList<string> errors, JSchemaValidationReader validationReader = null)
        {
            IList<string> messages = new List<string>();

            data.Validate(schema, (sender, args) => {                 
                messages.Add(args.Error.CreateFullMessage());             
            }, validationReader);

            errors = messages;
            return (errors.Count == 0);
        }

        public static void Validate(this JToken data, JSchema schema, ValidationErrorHandler handler, JSchemaValidationReader parentReader = null)
        {
            JSchemaValidationReader validationReader = new JSchemaValidationReader(parentReader);
            validationReader.ErrorHandled += handler;
            validationReader.Validate(data, schema);
        }

        public static bool IsValid(this JToken data, JSchema schema, JSchemaValidationReader validationReader = null)
        {
            bool valid = true;
            data.Validate(schema, (sender, args) => { valid = false; }, validationReader);
            return valid;
        }
    }
}
