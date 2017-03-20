using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace My.Json.Schema
{
    public static class SchemaExtensions
    {

        public static bool IsValid(this JToken data, JSchema schema, out IList<ValidationError> errors)
        {
            IList<ValidationError> messages = new List<ValidationError>();

            data.Validate(schema, (sender, args) => { messages.Add(args.Error); });

            errors = messages;
            return (errors.Count == 0);
        }

        public static bool IsValid(this JToken data, JSchema schema, out IList<string> errors)
        {
            IList<string> messages = new List<string>();

            data.Validate(schema, (sender, args) => {                 
                messages.Add(args.Error.CreateFullMessage());             
            });

            errors = messages;
            return (errors.Count == 0);
        }

        public static void Validate(this JToken data, JSchema schema, ValidationErrorHandler handler)
        {
            JSchemaValidationReader validationReader = new JSchemaValidationReader();
            validationReader.ErrorHandled += handler;
            validationReader.Validate(data, schema);
        }

        public static bool IsValid(this JToken data, JSchema schema)
        {
            bool valid = true;
            data.Validate(schema, (sender, args) => { valid = false; });
            return valid;
        }
    }
}
