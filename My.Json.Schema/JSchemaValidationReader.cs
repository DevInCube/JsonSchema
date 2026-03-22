using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace My.Json.Schema;

public class JSchemaValidationReader
{
    private const double SafePrecisionValue = 1e-7;

    public event EventHandler<ValidationEventArgs> ErrorHandled;

    private readonly JSchemaValidationReader _parentReader;
    private JSchema _schema;
    private JToken _data;

    public JSchemaValidationReader(JSchemaValidationReader parentReader)
    {
        _parentReader = parentReader;
    }

    public void Validate(JToken data, JSchema schema)
    {
        ArgumentNullException.ThrowIfNull(schema);

        JSchemaValidationReader reader = _parentReader;
        while (reader != null)
        {
            if (reader._data == data &&
                reader._schema == schema)
            {
                return;
            }

            reader = reader._parentReader;
        }

        if (data == null)
        {
            if (!schema.Type.HasFlag(JSchemaType.Null))
            {
                RaiseValidationError("Unexpected null");
            }

            return;
        }

        _data = data;
        _schema = schema;

        if (schema.Enum.Count > 0)
        {
            ValidateEnum();
        }

        ValidateType();

        if (schema.AllOf.Count > 0)
        {
            ValidateAllOf();
        }

        if (schema.AnyOf.Count > 0)
        {
            ValidateAnyOf();
        }

        if (schema.OneOf.Count > 0)
        {
            ValidateOneOf();
        }

        if (schema.Not != null)
        {
            ValidateNot();
        }

        //definitions
        //@todo parse and validate
    }

    private void ValidateEnum()
    {
        if (_schema.Enum.Any(enumItem => enumItem.IsEqualTo(_data)))
        {
            return;
        }

        RaiseValidationError("Data does not matches enum");
    }

    private void ValidateNot()
    {
        if (_data.IsValid(_schema.Not, this))
        {
            RaiseValidationError("Data should not be valid against the schema");
        }
    }

    private void ValidateOneOf()
    {
        var validSchemasCount = _schema.OneOf
            .Count(x => _data.IsValid(x, this));
        switch (validSchemasCount)
        {
            case 0:
                RaiseValidationError("Data is not valid against any schema");
                return;
            case 1:
                return;
            default:
                RaiseValidationError("Data is valid against more than one schema");
                return;
        }
    }

    private void ValidateAnyOf()
    {
        var isAnyValid = _schema.AnyOf
            .Any(x => _data.IsValid(x, this));
        if (!isAnyValid)
        {
            RaiseValidationError("Data is not valid against any schema");
        }
    }

    private void ValidateAllOf()
    {
        var isAllValid = _schema.AllOf
            .All(x => _data.IsValid(x, this));
        if (!isAllValid)
        {
            RaiseValidationError("Data is not valid against all of schemas");
        }
    }

    private void ValidateType()
    {
        void ValidateNullType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Null)
                || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected null value");
            }
        }

        void ValidateIntegerType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Integer)
                || _schema.Type.HasFlag(JSchemaType.Number)
                || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected integer value");
            }

            double integer = Convert.ToDouble(_data, CultureInfo.InvariantCulture);

            ValidateInteger(integer);
        }

        void ValidateFloatType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Number)
                || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected number value");
            }

            double doubleValue = _data.Value<double>();

            ValidateNumber(doubleValue);
        }

        void ValidateStringOrDateType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.String)
                || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected string value");
            }

            var value = _data.Type == JTokenType.Date
                ? _data.Value<DateTime>().ToJsonString()
                : _data.Value<string>();

            ValidateString(value);
        }

        void ValidateBooleanType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Boolean)
                 || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected boolean value");
            }
        }

        void ValidateArrayType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Array)
                || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected array");
            }

            JArray array = _data as JArray;

            ValidateArray(array);
        }

        void ValidateObjectType()
        {
            if (!(_schema.Type.HasFlag(JSchemaType.Object)
               || _schema.Type == JSchemaType.None))
            {
                RaiseValidationError("Unexpected object value");
            }

            JObject obj = _data as JObject;

            ValidateObject(obj);
        }

        if (_data.Type == JTokenType.Null)
        {
            ValidateNullType();
        }

        if (_data.Type == JTokenType.Integer)
        {
            ValidateIntegerType();
        }

        if (_data.Type == JTokenType.Float)
        {
            ValidateFloatType();
        }

        if (_data.Type == JTokenType.String
            || _data.Type == JTokenType.Date)
        {
            ValidateStringOrDateType();
        }

        if (_data.Type == JTokenType.Boolean)
        {
            ValidateBooleanType();
        }

        if (_data.Type == JTokenType.Array)
        {
            ValidateArrayType();
        }

        if (_data.Type == JTokenType.Object)
        {
            ValidateObjectType();
        }
    }

    private void ValidateString(string value)
    {
        int strLen = new StringInfo(value).LengthInTextElements;

        if (strLen < _schema.MinLength)
        {
            RaiseValidationError("String length is less than minimum");
        }

        if (strLen > _schema.MaxLength)
        {
            RaiseValidationError("String length is greater than maximum");
        }

        if (_schema.Pattern != null)
        {
            Regex regex = RegexHelpers.Create(_schema.Pattern);
            if (!regex.IsMatch(value))
            {
                RaiseValidationError("String does not matches pattern");
            }
        }

        if (_schema.Format != null)
        {
            ValidateStringFormat(value, _schema.Format);
        }
    }

    private void ValidateStringFormat(string value, string format)
    {
        static string GetValidationError(string value, string format)
        {
            return format switch
            {
                "date-time" when !DateTimeHelpers.IsValidDateTimeFormat(value) => "String is not in correct date-time format",
                "email" when !EMailHelpers.IsValidEmail(value) => "String is not in correct email format",
                "hostname" when !StringHelpers.IsValidHostName(value) => "String is not in correct hostname format",
                "ipv4" when !StringHelpers.IsValidIPv4(value) => "String is not in correct ipv4 format.",
                "ipv6" when Uri.CheckHostName(value) != UriHostNameType.IPv6 => "String is not in correct ipv6 format.",
                "uri" when !Uri.IsWellFormedUriString(value, UriKind.Absolute) => "String is not in correct uri format.",
                _ => null,
            };
        }

        var validationError = GetValidationError(value, format);
        if (!string.IsNullOrEmpty(validationError))
        {
            RaiseValidationError(validationError);
        }
    }

    private void ValidateNumber(double doubleValue)
    {
        void ValidateMinimum()
        {
            if (doubleValue < _schema.Minimum)
            {
                RaiseValidationError("Value is less than minimum");
            }

            if (_schema.ExclusiveMinimum
                && Math.Abs(doubleValue - _schema.Minimum.Value) <= SafePrecisionValue)
            {
                RaiseValidationError("Value should not be equal to minimum");
            }
        }

        void ValidateMaximum()
        {
            if (doubleValue > _schema.Maximum)
            {
                RaiseValidationError("Value is greater than maximum");
            }

            if (_schema.ExclusiveMaximum
                && Math.Abs(doubleValue - _schema.Maximum.Value) <= SafePrecisionValue)
            {
                RaiseValidationError("Value should not be equal to maximum");
            }
        }

        void ValidateMultipleOf()
        {
            try
            {
                if (Math.Abs(Math.IEEERemainder(doubleValue, _schema.MultipleOf.Value)) > SafePrecisionValue)
                {
                    RaiseValidationError("Value is not a multiple of");
                }
            }
            catch (OverflowException)
            {
                RaiseValidationError("Value overflow");
            }
        }

        if (_schema.Minimum != null)
        {
            ValidateMinimum();
        }

        if (_schema.Maximum != null)
        {
            ValidateMaximum();
        }

        if (_schema.MultipleOf != null)
        {
            ValidateMultipleOf();
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
            {
                RaiseValidationError("Array length is less than minimum");
            }
        }

        if (_schema.MaxItems != null)
        {
            if (array.Count > _schema.MaxItems)
            {
                RaiseValidationError("Array length is greater than maximum");
            }
        }

        if (_schema.ItemsSchema != null)
        {
            foreach (JToken item in array)
            {
                if (!item.IsValid(_schema.ItemsSchema, out IList<ValidationError> childErrors, this))
                {
                    RaiseValidationError("Array items are not valid against items schema", childErrors);
                }
            }
        }

        if (_schema.ItemsArray.Count > 0)
        {
            if ((array.Count > _schema.ItemsArray.Count)
                && !_schema.AllowAdditionalItems)
            {
                RaiseValidationError("Array length is greater than schema array length as additional items are not allowed");
            }

            for (int i = 0; i < array.Count; i++)
            {
                JToken item = array[i];
                var itemSchema = i < _schema.ItemsArray.Count
                    ? _schema.ItemsArray[i]
                    : _schema.AdditionalItems;

                if (!item.IsValid(itemSchema, out IList<ValidationError> childErrors, this))
                {
                    RaiseValidationError("Array item is not valid against schema", childErrors);
                }
            }
        }
    }

    private void ValidateUniqueItems(JArray array)
    {
        HashSet<JToken> uniqueItems = new(new Utilities.JTokenEqualityComparer());
        foreach (JToken item in array.Children())
        {
            if (uniqueItems.Contains(item))
            {
                RaiseValidationError("Array items are not unique");
                return;
            }

            uniqueItems.Add(item);
        }
    }

    private void ValidateObject(JObject obj)
    {
        foreach (string requiredName in _schema.Required)
        {
            bool exists = obj.Properties().Any(prop => prop.Name.Equals(requiredName, StringComparison.Ordinal));
            if (!exists)
            {
                RaiseValidationError("Required property is missing");
            }
        }

        if (_schema.MinProperties != null)
        {
            if (obj.Properties().Count() < _schema.MinProperties)
            {
                RaiseValidationError("Properties count is less than minimum");
            }
        }

        if (_schema.MaxProperties != null)
        {
            if (obj.Properties().Count() > _schema.MaxProperties)
            {
                RaiseValidationError("Properties count is greater than maximum");
            }
        }

        foreach (JProperty property in obj.Properties())
        {
            ValidateObjectProperty(property);
        }

        foreach (var pair in _schema.SchemaDependencies)
        {
            if (obj.TryGetValue(pair.Key, out _)
                && !obj.IsValid(pair.Value, this))
            {
                RaiseValidationError("Schema dependency is not valid");
            }
        }

        foreach ((string key, IList<string> propertySet) in _schema.PropertyDependencies)
        {
            if (!obj.TryGetValue(key, out JToken token))
            {
                continue;
            }

            foreach (string propertyName in propertySet)
            {
                if (!obj.TryGetValue(propertyName, out token))
                {
                    RaiseValidationError("Property dependency is not valid");
                }
            }
        }
    }

    private void ValidateObjectProperty(JProperty property)
    {
        IEnumerable<JSchema> GetSchemas(JProperty property)
        {
            IList<JSchema> schemas = [];
            string propName = property.Name;
            if (_schema.Properties.TryGetValue(propName, out JSchema value))
            {
                schemas.Add(value);
            }

            foreach (var patternPair in _schema.PatternProperties)
            {
                Regex nameRegex = RegexHelpers.Create(patternPair.Key);
                if (nameRegex.IsMatch(propName))
                {
                    schemas.Add(patternPair.Value);
                }
            }

            if (schemas.Count == 0)
            {
                if (!_schema.AllowAdditionalProperties)
                {
                    RaiseValidationError("Additional properties are not allowed");
                }

                schemas.Add(_schema.AdditionalProperties);
            }

            return schemas;
        }
       
        foreach (JSchema propSchema in GetSchemas(property))
        {
            if (!property.Value.IsValid(propSchema, out IList<ValidationError> childErrors, this))
            {
                RaiseValidationError("Property '{0}' is not valid against schema".FormatWith(property.Name), childErrors);
            }
        }
    }

    private void ValidateInteger(double integer)
    {
        if (_schema.Minimum != null)
        {
            if (integer < _schema.Minimum)
            {
                RaiseValidationError("Value is less than minimum");
            }

            if (_schema.ExclusiveMinimum)
            {
                if (Math.Abs(integer - _schema.Minimum.Value) > SafePrecisionValue)
                {
                    RaiseValidationError("Value should not be equal to minimum");
                }
            }
        }

        if (_schema.Maximum != null)
        {
            if (integer > _schema.Maximum)
            {
                RaiseValidationError("Value is greater than maximum");
            }

            if (_schema.ExclusiveMaximum)
            {
                if (Math.Abs(integer - _schema.Maximum.Value) > SafePrecisionValue)
                {
                    RaiseValidationError("Value should not be equal to maximum");
                }
            }
        }

        if (_schema.MultipleOf != null)
        {
            if (Math.Abs(integer) > SafePrecisionValue)
            {
                if (Math.Abs(integer % (double)_schema.MultipleOf) > SafePrecisionValue)
                {
                    RaiseValidationError("Value is not a multiple of");
                }
            }
        }
    }

    private void RaiseValidationError(string message, IList<ValidationError> childErrors = null)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentNullException(nameof(message));
        }

        if (ErrorHandled == null)
        {
            throw new JSchemaException(message);
        }

        ValidationError error = new(message, _data) { ChildErrors = childErrors };
        ValidationEventArgs args = new(error);
        ErrorHandled.Invoke(this, args);
    }
}
