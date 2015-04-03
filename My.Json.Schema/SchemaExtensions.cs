using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public static class SchemaExtensions
    {

        public static bool IsValid(this JToken data, JSchema schema)
        {
            if (data == null) return schema.Type.HasFlag(JSchemaType.Null);
            if (data is JValue)
            {
                //if (schema.Type == JSchemaType.None) return true;
                  
                if (data.Type == JTokenType.Integer)
                {
                    if (!schema.Type.HasFlag(JSchemaType.Integer)
                        || schema.Type.HasFlag(JSchemaType.Number))
                        return false;

                    int integer = data.Value<int>();

                    if (schema.Minimum != null)
                    {
                        if (integer < schema.Minimum) return false;
                        if (schema.ExclusiveMinimum)
                            if (integer == schema.Minimum) return false;
                    }
                    if (schema.Maximum != null)
                    {
                        if (integer > schema.Maximum) return false;
                        if (schema.ExclusiveMaximum)
                            if (integer == schema.Maximum) return false;
                    }

                    return true;
                }
                if (data.Type == JTokenType.Float)
                {
                    if (!(schema.Type.HasFlag(JSchemaType.Number) 
                        || schema.Type == JSchemaType.None)) return false;

                    double doubleValue = data.Value<double>();

                    if (schema.Minimum != null)
                    {
                        if (doubleValue < schema.Minimum) return false;
                        if (schema.ExclusiveMinimum)
                            if (doubleValue == schema.Minimum) return false;
                    }
                    if (schema.Maximum != null)
                    {
                        if (doubleValue > schema.Maximum) return false;
                        if (schema.ExclusiveMaximum)
                            if (doubleValue == schema.Maximum) return false;
                    }

                    return true;
                }

                if (data.Type == JTokenType.String)
                {
                    if (!(schema.Type.HasFlag(JSchemaType.String)
                        || schema.Type == JSchemaType.None)) return false;

                    string value = data.Value<string>();

                    if (schema.MinLength != null)
                    {
                        if (value.Length < schema.MinLength) return false;
                    }
                    if (schema.MaxLength != null)
                    {
                        if (value.Length > schema.MaxLength) return false;
                    }

                    if (schema.Format != null)
                    {
                        switch (schema.Format)
                        {
                            case ("email"): return EMailHelpers.IsValidEmail(value);
                        }
                    }
                    return true;
                }
                if (data.Type == JTokenType.Boolean) return schema.Type.HasFlag(JSchemaType.Boolean);
                if (schema.Type.HasFlag(JSchemaType.Object)
                    || schema.Type.HasFlag(JSchemaType.Array))
                    return false;
                return false;
            }
            else if (data.Type == JTokenType.Object)
            {
                if (schema.Type.HasFlag(JSchemaType.Object) || schema.Type == JSchemaType.None)
                {
                    JObject obj = data as JObject;

                    if (schema.MaxProperties != null)
                    {
                        if (obj.Properties().Count() > schema.MaxProperties) return false;
                    }

                    if (schema.MinProperties != null)
                    {
                        if (obj.Properties().Count() < schema.MinProperties) return false;
                    }

                    return true;

                    //@todo check properties
                    //@todo check inner schemas
                }
                else
                    return false;
            }
            else if (data.Type == JTokenType.Array)
            {
                if (schema.Type.HasFlag(JSchemaType.Array) || schema.Type == JSchemaType.None)
                {
                    JArray arr = data as JArray;

                    if (schema.MaxItems != null)
                    {
                        if (arr.Children().Count() > schema.MaxItems) return false;
                    }

                    if (schema.MinItems != null)
                    {
                        if (arr.Children().Count() < schema.MinItems) return false;
                    }

                    return true;

                    //@todo check array
                    //@todo check array items
                }
                else
                    return false;
            }
            else
                return false;                       
        }
    }
}
