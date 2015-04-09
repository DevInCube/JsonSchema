using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace My.Json.Schema
{
    public static class SchemaKeywords
    {
        public const string Type = "type";
        public const string Properties = "properties";
        public const string Items = "items";
        public const string AdditionalItems = "additionalItems";
        public const string Required = "required";
        public const string PatternProperties = "patternProperties";
        public const string AdditionalProperties = "additionalProperties";
        public const string Dependencies = "dependencies";
        public const string Definitions = "definitions";
        public const string Minimum = "minimum";
        public const string Maximum = "maximum";
        public const string ExclusiveMinimum = "exclusiveMinimum";
        public const string ExclusiveMaximum = "exclusiveMaximum";
        public const string MinimumItems = "minItems";
        public const string MaximumItems = "maxItems";
        public const string Pattern = "pattern";
        public const string MaximumLength = "maxLength";
        public const string MinimumLength = "minLength";
        public const string Enum = "enum";        
        public const string Title = "title";
        public const string Description = "description";
        public const string Format = "format";
        public const string Default = "default";                
        public const string MultipleOf = "multipleOf";        
        public const string Id = "id";
        public const string UniqueItems = "uniqueItems";
        public const string MinimumProperties = "minProperties";
        public const string MaximumProperties = "maxProperties";

        public const string AnyOf = "anyOf";
        public const string AllOf = "allOf";
        public const string OneOf = "oneOf";
        public const string Not = "not";

        public const string Ref = "$ref";
        public const string Schema = "$schema";
    }
}
