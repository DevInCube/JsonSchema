using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace My.Json.Schema
{
    public static class SchemaExtensions
    {

        public static bool IsValid(this JToken data, JSchema schema)
        {
            if (data == null) return schema.Type.HasFlag(JSchemaType.Null);

            //enum
            if (schema.Enum.Count > 0)
            {
                bool matchesEnum = false;
                foreach (JToken enumItem in schema.Enum)
                    if (JToken.DeepEquals(enumItem, data))
                    {
                        matchesEnum = true;
                        break;
                    }
                if (!matchesEnum) return false;
            }

            //type
            bool validType = false;

            if (data.Type == JTokenType.Null)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Null)
                    || schema.Type == JSchemaType.None)) return false;

                validType = true;
            }

            if (data.Type == JTokenType.Integer)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Integer)
                    || schema.Type.HasFlag(JSchemaType.Number)
                    || schema.Type == JSchemaType.None))
                    return false;

                double integer;

                try
                {
                    integer = (double)data.Value<int>();
                }
                catch (InvalidCastException)
                {
                    integer = Convert.ToDouble(data);
                }

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
                if (schema.MultipleOf != null)
                {
                    if (integer != 0)
                    {
                        if (integer % schema.MultipleOf != 0) return false;
                    }
                }

                validType = true;
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
                if (schema.MultipleOf != null)
                {
                    if (doubleValue != 0)
                    {
                        decimal value = (decimal)doubleValue;
                        decimal multiple = (decimal)schema.MultipleOf;
                        if (value % multiple != 0) return false;
                    }
                }

                validType = true;
            }

            if (data.Type == JTokenType.String
                || data.Type == JTokenType.Date)
            {
                if (!(schema.Type.HasFlag(JSchemaType.String)
                     || schema.Type == JSchemaType.None)) return false;

                string value;
                if (data.Type == JTokenType.Date)                
                    value = data.Value<DateTime>().ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'");
                else
                    value = data.Value<string>();

                int strLen = new StringInfo(value).LengthInTextElements;

                if (schema.MinLength != null)
                {
                    if (strLen < schema.MinLength) return false;
                }
                if (schema.MaxLength != null)
                {
                    if (strLen > schema.MaxLength) return false;
                }

                if (schema.Pattern != null)
                {
                    Regex regex = new Regex(schema.Pattern);
                    if (!regex.IsMatch(value)) return false;
                }

                if (schema.Format != null)
                {
                    switch (schema.Format)
                    {
                        case ("date-time"):
                            {
                                DateTime temp;
                                if (!DateTime.TryParseExact(value, "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff'Z'", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out temp))
                                    return false;               
                                break;
                            }
                        case ("email"):
                            {
                                if (!EMailHelpers.IsValidEmail(value))
                                    return false;
                                break;
                            }
                        case ("hostname"):
                            {
                                if (!Regex.IsMatch(value, @"^(?=.{1,255}$)[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?(?:\.[0-9A-Za-z](?:(?:[0-9A-Za-z]|-){0,61}[0-9A-Za-z])?)*\.?$", RegexOptions.CultureInvariant))
                                    return false;
                                break;
                            }
                        case ("ipv4"):
                            {
                                string[] parts = value.Split('.');
                                if (parts.Length != 4)
                                    return false;

                                for (int i = 0; i < parts.Length; i++)
                                {
                                    int num;
                                    if (!int.TryParse(parts[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out num)
                                        || (num < 0 || num > 255))
                                    {
                                        return false;
                                    }
                                }                                
                                break;
                            }
                        case ("ipv6"):
                            {
                                if (!(Uri.CheckHostName(value) == UriHostNameType.IPv6))
                                    return false;
                                break;
                            }
                        case ("uri"):
                            {
                                if (!Uri.IsWellFormedUriString(value, UriKind.Absolute))
                                    return false;
                                break;
                            }
                    }
                }

                validType = true;
            }
            if (data.Type == JTokenType.Boolean)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Boolean)
                     || schema.Type == JSchemaType.None)) return false;

                validType = true;
            }
            if (data.Type == JTokenType.Array)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Array)
                    || schema.Type == JSchemaType.None)) return false;

                JArray array = data as JArray;

                if (schema.UniqueItems)
                {
                    IList<JToken> uniques = new List<JToken>();
                    foreach (JToken item in array.Children())
                    {
                        foreach (JToken unique in uniques)
                            if (JToken.DeepEquals(item, unique)) return false;
                        uniques.Add(item);
                    }

                }

                if (schema.MinItems != null)
                {
                    if (array.Count < schema.MinItems) return false;
                }

                if (schema.MaxItems != null)
                {
                    if (array.Count > schema.MaxItems) return false;
                }

                if (schema.ItemsSchema != null)
                {
                    foreach (JToken item in array)
                        if (!item.IsValid(schema.ItemsSchema)) return false;
                }

                if (schema.ItemsArray.Count > 0)
                {
                    if ((array.Count > schema.ItemsArray.Count)
                        && !schema.AllowAdditionalItems)
                        return false;
                    for (int i = 0; i < array.Count; i++)
                    {
                        JToken item = array[i];
                        JSchema itemSchema;
                        if (i < schema.ItemsArray.Count)
                        {
                            itemSchema = schema.ItemsArray[i];
                        }
                        else
                        {                       
                            itemSchema = schema.AdditionalItems;
                        }
                        if(!item.IsValid(itemSchema)) return false;
                    }
                }

                validType = true;
            }
            if (data.Type == JTokenType.Object)
            {
                if (!(schema.Type.HasFlag(JSchemaType.Object)
                    || schema.Type == JSchemaType.None)) return false;

                JObject obj = data as JObject;                

                foreach(string requiredName in schema.Required){
                    bool exists = false;
                    foreach (JProperty prop in obj.Properties())
                        if (prop.Name.Equals(requiredName))
                        {
                            exists = true;
                            break;
                        }
                    if (!exists) return false;
                 }

                if (schema.MinProperties != null)
                {
                    if (obj.Properties().Count() < schema.MinProperties) return false;
                }

                if (schema.MaxProperties != null)
                {
                    if (obj.Properties().Count() > schema.MaxProperties) return false;
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
                        if (!schema.AllowAdditionalProperties) return false;
                        s.Add(schema.AdditionalProperties);
                    }
                    foreach (JSchema propSchema in s)
                        if (!prop.Value.IsValid(propSchema)) return false;
                }

                if (schema.SchemaDependencies.Count > 0)
                {
                    foreach(var pair in schema.SchemaDependencies)
                    {
                        JToken t;
                        if (obj.TryGetValue(pair.Key, out t))
                        {
                            if (!obj.IsValid(pair.Value)) return false;
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
                                if (!obj.TryGetValue(propertyName, out t)) return false;
                        }
                    }
                }

                validType = true;
            }

            if (!validType) return false;
            
            //all of
            if (schema.AllOf.Count > 0)
            {
                foreach (JSchema allOfSchema in schema.AllOf)
                    if (!data.IsValid(allOfSchema)) return false;
            }

            //any of 
            if (schema.AnyOf.Count > 0)
            {
                bool validAnyOf = false;
                foreach (JSchema anyOfSchema in schema.AnyOf)
                    if (data.IsValid(anyOfSchema))
                    {
                        validAnyOf = true;
                        break;
                    }
                if (!validAnyOf) return false;
            }

            // one of
            if (schema.OneOf.Count > 0)
            {
                bool validOneOf = false;
                foreach (JSchema oneOfSchema in schema.OneOf)
                    if (data.IsValid(oneOfSchema))
                    {
                        if (!validOneOf) validOneOf = true;
                        else return false;
                    }
                if (!validOneOf) return false;
            }

            // not 
            if (schema.Not != null)
            {
                if (data.IsValid(schema.Not)) return false;
            }

            //definiitions
            //@todo parse and validate

            return true;
        }
    }
}
