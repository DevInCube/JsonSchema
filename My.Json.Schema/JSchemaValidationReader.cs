using My.Json.Schema.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace My.Json.Schema
{
    public class JSchemaValidationReader
    {

        public event ValidationErrorHandler ErrorHandled;

        private JSchema schema;
        private JToken data;

        public JSchemaValidationReader() { }

        public void Validate(JToken data, JSchema schema)
        {            
            if (schema == null) throw new ArgumentNullException("schema");

            this.data = data;
            this.schema = schema;

            if (data == null)
            {
                if(!schema.Type.HasFlag(JSchemaType.Null))
                    RaiseValidationError("Unexpected null");
                return;
            }

            if (schema.Enum.Count > 0)
                ValidateEnum();
            ValidateType();
            if (schema.AllOf.Count > 0)
                ValidateAllOf();
            if (schema.AnyOf.Count > 0)
                ValidateAnyOf();
            if (schema.OneOf.Count > 0)
                ValidateOneOf();
            if (schema.Not != null)
                ValidateNot();
            
            //definitions
            //@todo parse and validate
        }

        private void ValidateEnum()
        {
            foreach (JToken enumItem in schema.Enum)
                if (JToken.DeepEquals(enumItem, data))
                    return;
            RaiseValidationError("Data does not matches enum");
        }

        private void ValidateNot()
        {
            if (data.IsValid(schema.Not))
                RaiseValidationError("Data should not be valid against the schema");
        }

        private void ValidateOneOf()
        {
            bool validOneOf = false;
            foreach (JSchema oneOfSchema in schema.OneOf)
                if (data.IsValid(oneOfSchema))
                {
                    if (!validOneOf) validOneOf = true;
                    else
                    {
                        RaiseValidationError("Data is valid against more than one schema");
                        return;
                    }
                }
            if (!validOneOf)
                RaiseValidationError("Data is not valid against any schema");
        }

        private void ValidateAnyOf()
        {
            foreach (JSchema anyOfSchema in schema.AnyOf)
                if (data.IsValid(anyOfSchema))
                    return;
            RaiseValidationError("Data is not valid against any schema");
        }

        private void ValidateAllOf()
        {
            foreach (JSchema allOfSchema in schema.AllOf)
                if (!data.IsValid(allOfSchema))
                    RaiseValidationError("Data is not valid against all of schemas");
        }

        private void ValidateType()
        {
            if (data.Type == JTokenType.Null)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Null)
                    || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected null value");
            }

            if (data.Type == JTokenType.Integer)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Integer)
                    || schema.Type.HasFlag(JSchemaType.Number)
                    || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected integer value");

                double integer = Convert.ToDouble(data);

                ValidateInteger(integer);
            }
            if (data.Type == JTokenType.Float)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Number)
                     || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected number value");

                double doubleValue = data.Value<double>();

                ValidateNumber(doubleValue);
            }

            if (data.Type == JTokenType.String
                || data.Type == JTokenType.Date)
            {
                if (!(schema.Type.HasFlag(JSchemaType.String)
                     || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected string value");

                string value;
                if (data.Type == JTokenType.Date)
                    value = data.Value<DateTime>().ToJsonString();
                else
                    value = data.Value<string>();

                ValidateString(value);
            }
            if (data.Type == JTokenType.Boolean)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Boolean)
                     || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected boolean value");                
            }
            if (data.Type == JTokenType.Array)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Array)
                    || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected array");

                JArray array = data as JArray;

                ValidateArray(array);
            }
            if (data.Type == JTokenType.Object)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Object)
                   || schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected object value");

                JObject obj = data as JObject;

                ValidateObject(obj);
            }            
        }

        private void ValidateString(string value)
        {
            int strLen = new StringInfo(value).LengthInTextElements;

            if (schema.MinLength != null)
            {
                if (strLen < schema.MinLength) 
                    RaiseValidationError("String length is less than minimum");
            }
            if (schema.MaxLength != null)
            {
                if (strLen > schema.MaxLength) 
                    RaiseValidationError("String length is greater than maximum");
            }

            if (schema.Pattern != null)
            {
                Regex regex = new Regex(schema.Pattern);
                if (!regex.IsMatch(value)) 
                    RaiseValidationError("String does not matches pattern");
            }

            if (schema.Format != null)
            {
                ValidateStringFormat(value, schema.Format);
            }
        }

        private void ValidateStringFormat(string value, string format)
        {
            switch (format)
            {
                case ("date-time"):
                    {
                        if(!DateTimeHelpers.IsValidDateTimeFormat(value))
                            RaiseValidationError("String is not in correct date-time format");
                        break;
                    }
                case ("email"):
                    {
                        if (!EMailHelpers.IsValidEmail(value))
                            RaiseValidationError("String is not in correct email format");
                        break;
                    }
                case ("hostname"):
                    {
                        if (!StringHelpers.IsValidHostName(value))
                            RaiseValidationError("String is not in correct hostname format");
                        break;
                    }
                case ("ipv4"):
                    {
                        if (!StringHelpers.IsValidIPv4(value))
                            RaiseValidationError("String is not in correct ipv4 format.");
                        break;
                    }
                case ("ipv6"):
                    {
                        if (!(Uri.CheckHostName(value) == UriHostNameType.IPv6))
                            RaiseValidationError("String is not in correct ipv6 format.");
                        break;
                    }
                case ("uri"):
                    {
                        if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                            RaiseValidationError("String is not in correct uri format.");
                        break;
                    }
            }
        }

        private void ValidateNumber(double doubleValue)
        {
            if (schema.Minimum != null)
            {
                if (doubleValue < schema.Minimum)
                    RaiseValidationError("Value is less than minimum");
                if (schema.ExclusiveMinimum)
                    if (doubleValue == schema.Minimum)
                        RaiseValidationError("Value should not be equal to minimum");
            }
            if (schema.Maximum != null)
            {
                if (doubleValue > schema.Maximum) RaiseValidationError("Value is greater than maximum");
                if (schema.ExclusiveMaximum)
                    if (doubleValue == schema.Maximum)
                        RaiseValidationError("Value should not be equal to maximum");
            }
            if (schema.MultipleOf != null)
            {
                if (doubleValue != 0)
                {
                    decimal value = (decimal)doubleValue;
                    decimal multiple = (decimal)schema.MultipleOf;
                    if (value % multiple != 0)
                        RaiseValidationError("Value is not a multiple of");
                }
            }
        }

        private void ValidateArray(JArray array)
        {            
            if (schema.UniqueItems)
            {
                ValidateUniqueItems(array);
            }

            if (schema.MinItems != null)
            {
                if (array.Count < schema.MinItems)
                    RaiseValidationError("Array length is less than minimum");
            }

            if (schema.MaxItems != null)
            {
                if (array.Count > schema.MaxItems)
                    RaiseValidationError("Array length is greater than maximum");
            }

            if (schema.ItemsSchema != null)
            {
                foreach (JToken item in array)
                {
                    IList<ValidationError> childErrors;
                    if (!item.IsValid(schema.ItemsSchema, out childErrors))
                        RaiseValidationError("Array items are not valid against items schema", childErrors);
                }
            }

            if (schema.ItemsArray.Count > 0)
            {
                if ((array.Count > schema.ItemsArray.Count)
                    && !schema.AllowAdditionalItems)
                    RaiseValidationError("Array length is greater than schema array length as additional items are not allowed");

                for (int i = 0; i < array.Count; i++)
                {
                    JToken item = array[i];
                    JSchema itemSchema;
                    if (i < schema.ItemsArray.Count)
                        itemSchema = schema.ItemsArray[i];
                    else
                        itemSchema = schema.AdditionalItems;

                    IList<ValidationError> childErrors;
                    if (!item.IsValid(itemSchema, out childErrors))
                        RaiseValidationError("Array item is not valid against schema", childErrors);
                }
            }
        }

        private void ValidateUniqueItems(JArray array)
        {
            IList<JToken> uniques = new List<JToken>();
            foreach (JToken item in array.Children())
            {
                foreach (JToken unique in uniques)
                    if (JToken.DeepEquals(item, unique))
                    {
                        RaiseValidationError("Array items are not unique");
                        return;
                    }
                uniques.Add(item);
            }            
        }

        private void ValidateObject(JObject obj)
        {
            foreach (string requiredName in schema.Required)
            {
                bool exists = false;
                foreach (JProperty prop in obj.Properties())
                    if (prop.Name.Equals(requiredName))
                    {
                        exists = true;
                        break;
                    }
                if (!exists)
                    RaiseValidationError("Required property is missing");
            }

            if (schema.MinProperties != null)
            {
                if (obj.Properties().Count() < schema.MinProperties)
                    RaiseValidationError("Properties count is less than minimum");
            }

            if (schema.MaxProperties != null)
            {
                if (obj.Properties().Count() > schema.MaxProperties)
                    RaiseValidationError("Properties count is greater than maximum");
            }

            foreach (JProperty prop in obj.Properties())
            {
                IList<JSchema> s = new List<JSchema>();
                string m = prop.Name;
                if (schema.Properties.ContainsKey(m))
                    s.Add(schema.Properties[m]);
                foreach (var patternPair in schema.PatternProperties)
                {
                    Regex nameRegex = new Regex(patternPair.Key);
                    if (nameRegex.IsMatch(m))
                        s.Add(patternPair.Value);
                }
                if (s.Count == 0)
                {
                    if (!schema.AllowAdditionalProperties)
                        RaiseValidationError("Additional properties are not allowed");
                    s.Add(schema.AdditionalProperties);
                }
                foreach (JSchema propSchema in s)
                {
                    IList<ValidationError> childErrors;
                    if (!prop.Value.IsValid(propSchema, out childErrors))
                        RaiseValidationError("Property {0} is not valid against schema".FormatWith(prop.Name), childErrors);
                }
            }

            if (schema.SchemaDependencies.Count > 0)
            {
                foreach (var pair in schema.SchemaDependencies)
                {
                    JToken t;
                    if (obj.TryGetValue(pair.Key, out t))
                    {
                        if (!obj.IsValid(pair.Value))
                            RaiseValidationError("Schema dependency is not valid");
                    }
                }
            }

            if (schema.PropertyDependencies.Count > 0)
            {
                foreach (var pair in schema.PropertyDependencies)
                {
                    JToken t;
                    if (obj.TryGetValue(pair.Key, out t))
                    {
                        IList<string> propertyset = pair.Value;
                        foreach (string propertyName in propertyset)
                            if (!obj.TryGetValue(propertyName, out t))
                                RaiseValidationError("Property dependency is not valid");
                    }
                }
            }
        }

        private void ValidateInteger(double integer)
        {
            if (schema.Minimum != null)
            {
                if (integer < schema.Minimum) RaiseValidationError("Value is less than minimum");
                if (schema.ExclusiveMinimum)
                    if (integer == schema.Minimum)
                        RaiseValidationError("Value should not be equal to minimum");
            }
            if (schema.Maximum != null)
            {
                if (integer > schema.Maximum) RaiseValidationError("Value is greater than maximum");
                if (schema.ExclusiveMaximum)
                    if (integer == schema.Maximum)
                        RaiseValidationError("Value should not be equal to maximum");
            }
            if (schema.MultipleOf != null)
            {
                if (integer != 0)
                {
                    if (integer % schema.MultipleOf != 0)
                        RaiseValidationError("Value is not a multiple of");
                }
            }
        }       

        private void RaiseValidationError(string message, IList<ValidationError> childErrors = null)
        {
            if (String.IsNullOrWhiteSpace(message)) throw new ArgumentNullException("message");

            ValidationErrorHandler handler = ErrorHandled;
            if (handler != null)
            {                
                ValidationError error = new ValidationError(message, data);
                error.ChildErrors = childErrors;
                ValidationEventArgs args = new ValidationEventArgs(error);
                ErrorHandled(this, args);
            }
            else
            {
                throw new JSchemaException(message);
            }
        }
    }
}
