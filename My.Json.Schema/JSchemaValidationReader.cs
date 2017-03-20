using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace My.Json.Schema
{
    public class JSchemaValidationReader
    {

        public event ValidationErrorHandler ErrorHandled;

        private JSchema _schema;
        private JToken _data;

        public JSchemaValidationReader() { }

        public void Validate(JToken data, JSchema schema)
        {            
            if (schema == null) throw new ArgumentNullException("schema");

            this._data = data;
            this._schema = schema;

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
            if (_schema.Enum.Any(enumItem => JToken.DeepEquals(enumItem, _data)))
            {
                return;
            }
            RaiseValidationError("Data does not matches enum");
        }

        private void ValidateNot()
        {
            if (_data.IsValid(_schema.Not))
                RaiseValidationError("Data should not be valid against the schema");
        }

        private void ValidateOneOf()
        {
            bool validOneOf = false;
            foreach (JSchema oneOfSchema in _schema.OneOf)
            {
                if (_data.IsValid(oneOfSchema))
                {
                    if (!validOneOf) validOneOf = true;
                    else
                    {
                        RaiseValidationError("Data is valid against more than one schema");
                        return;
                    }
                }
            }
            if (!validOneOf)
            {
                RaiseValidationError("Data is not valid against any schema");
            }
        }

        private void ValidateAnyOf()
        {
            foreach (JSchema anyOfSchema in _schema.AnyOf)
                if (_data.IsValid(anyOfSchema))
                    return;
            RaiseValidationError("Data is not valid against any schema");
        }

        private void ValidateAllOf()
        {
            foreach (JSchema allOfSchema in _schema.AllOf)
                if (!_data.IsValid(allOfSchema))
                    RaiseValidationError("Data is not valid against all of schemas");
        }

        private void ValidateType()
        {
            if (_data.Type == JTokenType.Null)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Null)
                    || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected null value");
            }

            if (_data.Type == JTokenType.Integer)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Integer)
                    || _schema.Type.HasFlag(JSchemaType.Number)
                    || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected integer value");

                double integer = Convert.ToDouble(_data);

                ValidateInteger(integer);
            }
            if (_data.Type == JTokenType.Float)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Number)
                     || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected number value");

                double doubleValue = _data.Value<double>();

                ValidateNumber(doubleValue);
            }

            if (_data.Type == JTokenType.String
                || _data.Type == JTokenType.Date)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.String)
                     || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected string value");

                var value = _data.Type == JTokenType.Date 
                    ? _data.Value<DateTime>().ToJsonString() 
                    : _data.Value<string>();

                ValidateString(value);
            }
            if (_data.Type == JTokenType.Boolean)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Boolean)
                     || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected boolean value");                
            }
            if (_data.Type == JTokenType.Array)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Array)
                    || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected array");

                JArray array = _data as JArray;

                ValidateArray(array);
            }
            if (_data.Type == JTokenType.Object)
            {
                if (!(_schema.Type.HasFlag(JSchemaType.Object)
                   || _schema.Type == JSchemaType.None))
                    RaiseValidationError("Unexpected object value");

                JObject obj = _data as JObject;

                ValidateObject(obj);
            }            
        }

        private void ValidateString(string value)
        {
            int strLen = new StringInfo(value).LengthInTextElements;

            if (strLen < _schema.MinLength) 
                RaiseValidationError("String length is less than minimum");
            if (strLen > _schema.MaxLength) 
                RaiseValidationError("String length is greater than maximum");

            if (_schema.Pattern != null)
            {
                Regex regex = new Regex(_schema.Pattern);
                if (!regex.IsMatch(value)) 
                    RaiseValidationError("String does not matches pattern");
            }

            if (_schema.Format != null)
            {
                ValidateStringFormat(value, _schema.Format);
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
                        if (Uri.CheckHostName(value) != UriHostNameType.IPv6)
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
            if (_schema.Minimum != null)
            {
                if (doubleValue < _schema.Minimum)
                    RaiseValidationError("Value is less than minimum");
                if (_schema.ExclusiveMinimum)
                    if (doubleValue == _schema.Minimum)
                        RaiseValidationError("Value should not be equal to minimum");
            }
            if (_schema.Maximum != null)
            {
                if (doubleValue > _schema.Maximum) RaiseValidationError("Value is greater than maximum");
                if (_schema.ExclusiveMaximum)
                    if (doubleValue == _schema.Maximum)
                        RaiseValidationError("Value should not be equal to maximum");
            }
            if (_schema.MultipleOf != null)
            {
                if (Math.Abs(doubleValue) > 1e-7)
                {
                    decimal value = (decimal)doubleValue;
                    decimal multiple = (decimal)_schema.MultipleOf;
                    if (value % multiple != 0)
                        RaiseValidationError("Value is not a multiple of");
                }
            }
        }

        private void ValidateArray(JArray array)
        {            
            if (_schema.UniqueItems)
            {
                ValidateUniqueItems(array);
            }

            if (_schema.MinItems != null)
            {
                if (array.Count < _schema.MinItems)
                    RaiseValidationError("Array length is less than minimum");
            }

            if (_schema.MaxItems != null)
            {
                if (array.Count > _schema.MaxItems)
                    RaiseValidationError("Array length is greater than maximum");
            }

            if (_schema.ItemsSchema != null)
            {
                foreach (JToken item in array)
                {
                    IList<ValidationError> childErrors;
                    if (!item.IsValid(_schema.ItemsSchema, out childErrors))
                        RaiseValidationError("Array items are not valid against items schema", childErrors);
                }
            }

            if (_schema.ItemsArray.Count > 0)
            {
                if ((array.Count > _schema.ItemsArray.Count)
                    && !_schema.AllowAdditionalItems)
                    RaiseValidationError("Array length is greater than schema array length as additional items are not allowed");

                for (int i = 0; i < array.Count; i++)
                {
                    JToken item = array[i];
                    var itemSchema = i < _schema.ItemsArray.Count 
                        ? _schema.ItemsArray[i] 
                        : _schema.AdditionalItems;

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
                {
                    if (JToken.DeepEquals(item, unique))
                    {
                        RaiseValidationError("Array items are not unique");
                        return;
                    }
                }
                uniques.Add(item);
            }            
        }

        private void ValidateObject(JObject obj)
        {
            foreach (string requiredName in _schema.Required)
            {
                bool exists = false;
                foreach (JProperty prop in obj.Properties())
                {
                    if (prop.Name.Equals(requiredName))
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    RaiseValidationError("Required property is missing");
                }
            }

            if (_schema.MinProperties != null)
            {
                if (obj.Properties().Count() < _schema.MinProperties)
                    RaiseValidationError("Properties count is less than minimum");
            }

            if (_schema.MaxProperties != null)
            {
                if (obj.Properties().Count() > _schema.MaxProperties)
                    RaiseValidationError("Properties count is greater than maximum");
            }

            foreach (JProperty property in obj.Properties())
            {
                IList<JSchema> schemas = new List<JSchema>();
                string propName = property.Name;
                if (_schema.Properties.ContainsKey(propName))
                    schemas.Add(_schema.Properties[propName]);
                foreach (var patternPair in _schema.PatternProperties)
                {
                    Regex nameRegex = new Regex(patternPair.Key);
                    if (nameRegex.IsMatch(propName))
                        schemas.Add(patternPair.Value);
                }
                if (schemas.Count == 0)
                {
                    if (!_schema.AllowAdditionalProperties)
                        RaiseValidationError("Additional properties are not allowed");
                    schemas.Add(_schema.AdditionalProperties);
                }
                foreach (JSchema propSchema in schemas)
                {
                    IList<ValidationError> childErrors;
                    if (!property.Value.IsValid(propSchema, out childErrors))
                        RaiseValidationError("Property '{0}' is not valid against schema".FormatWith(property.Name), childErrors);
                }
            }

            if (_schema.SchemaDependencies.Count > 0)
            {
                foreach (var pair in _schema.SchemaDependencies)
                {
                    JToken t;
                    if (obj.TryGetValue(pair.Key, out t))
                    {
                        if (!obj.IsValid(pair.Value))
                            RaiseValidationError("Schema dependency is not valid");
                    }
                }
            }

            if (_schema.PropertyDependencies.Count > 0)
            {
                foreach (var pair in _schema.PropertyDependencies)
                {
                    JToken token;
                    if (obj.TryGetValue(pair.Key, out token))
                    {
                        IList<string> propertyset = pair.Value;
                        foreach (string propertyName in propertyset)
                        {
                            if (!obj.TryGetValue(propertyName, out token))
                                RaiseValidationError("Property dependency is not valid");
                        }
                    }
                }
            }
        }

        private void ValidateInteger(double integer)
        {
            if (_schema.Minimum != null)
            {
                if (integer < _schema.Minimum) RaiseValidationError("Value is less than minimum");
                if (_schema.ExclusiveMinimum)
                    if (integer == _schema.Minimum)
                        RaiseValidationError("Value should not be equal to minimum");
            }
            if (_schema.Maximum != null)
            {
                if (integer > _schema.Maximum) RaiseValidationError("Value is greater than maximum");
                if (_schema.ExclusiveMaximum)
                    if (integer == _schema.Maximum)
                        RaiseValidationError("Value should not be equal to maximum");
            }
            if (_schema.MultipleOf != null)
            {
                if (Math.Abs(integer) > 1e-7)
                {
                    if (integer % _schema.MultipleOf != 0)
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
                ValidationError error = new ValidationError(message, _data) {ChildErrors = childErrors};
                ValidationEventArgs args = new ValidationEventArgs(error);
                if (ErrorHandled != null) 
                    ErrorHandled(this, args);
            }
            else
            {
                throw new JSchemaException(message);
            }
        }
    }
}
