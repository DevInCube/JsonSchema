using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using My.Json.Schema.Utilities;

namespace My.Json.Schema
{
    public class JSchema
    {

        internal JObject Schema;

        private Uri _id;
        private JSchema _itemsSchema;
        private JSchema _additionalItems;
        private JSchema _additionalProperties;
        private IDictionary<string, JSchema> _properties;
        private IDictionary<string, JSchema> _pattternProperties;
        private IDictionary<string, JSchema> _schemaDependencies;
        private string _pattern;
        private double? _multipleOf;
        private int? _maxLength;
        private int? _minLength;
        private int? _maxItems;
        private int? _minItems;
        private int? _maxProperties;
        private int? _minProperties;
        private IList<JToken> _enum;
        private IList<string> _required;
        private IList<JSchema> _allOf;
        private IList<JSchema> _anyOf;
        private IList<JSchema> _oneOf;
        private IList<JSchema> _itemsArray;
        private IDictionary<string, IList<string>> _propertyDependencies;
        private IDictionary<string, JToken> _extensionData;

        #region public properties

        public Uri Id
        {
            get { return _id; }
            set
            {
                _id = value;
                if (!Id.IsAbsoluteUri)
                {
                    if (String.IsNullOrWhiteSpace(_id.OriginalString)
                        || _id.OriginalString.Equals("#"))
                        throw new JSchemaException("invalid id : {0}".FormatWith(Id));
                }
            }
        }

        public JSchemaType Type { get; set; }

        public IDictionary<string, JSchema> Properties
        {
            get { return _properties ?? (_properties = new Dictionary<string, JSchema>()); }
        }

        public IDictionary<string, JSchema> PatternProperties
        {
            get { return _pattternProperties ?? (_pattternProperties = new Dictionary<string, JSchema>()); }
        }

        public string Title { get; set; }
        public string Description { get; set; }
        public object Default { get; set; }
        public string Format { get; set; }

        public JSchema ItemsSchema
        {
            get { return _itemsSchema ?? (_itemsSchema = new JSchema()); }
            set { _itemsSchema = value; }
        }

        public IList<JSchema> ItemsArray
        {
            get { return _itemsArray ?? (_itemsArray = new List<JSchema>()); }
        }

        public double? MultipleOf
        {
            get { return _multipleOf; }
            set
            {
                if (value <= 0) throw new JSchemaException("multipleOf should be greater than zero");
                _multipleOf = value;
            }
        }

        public double? Maximum { get; set; }
        public double? Minimum { get; set; }
        public bool ExclusiveMaximum { get; set; }
        public bool ExclusiveMinimum { get; set; }

        public int? MaxLength
        {
            get { return _maxLength; }
            set
            {
                if (value < 0) throw new JSchemaException("maxLength should be greater or equal zero");
                _maxLength = value;
            }
        }

        public int? MinLength
        {
            get { return _minLength; }
            set
            {
                if (value < 0) throw new JSchemaException("minLength should be greater or equal zero");
                _minLength = value;
            }
        }

        public string Pattern
        {
            get { return _pattern; }
            set
            {
                _pattern = value;
                if (!StringHelpers.IsValidRegex(_pattern))
                    throw new JSchemaException("pattern is not a valid regex string");
            }
        }

        public int? MaxItems
        {
            get { return _maxItems; }
            set
            {
                if (value < 0) throw new JSchemaException("maxItems should be greater or equal zero");
                _maxItems = value;
            }
        }

        public int? MinItems
        {
            get { return _minItems; }
            set
            {
                if (value < 0) throw new JSchemaException("minItems should be greater or equal zero");
                _minItems = value;
            }
        }

        public bool UniqueItems { get; set; }

        public int? MaxProperties
        {
            get { return _maxProperties; }
            set
            {
                if (value < 0) throw new JSchemaException("maxProperties should be greater or equal zero");
                _maxProperties = value;
            }
        }

        public int? MinProperties
        {
            get { return _minProperties; }
            set
            {
                if (value < 0) throw new JSchemaException("minProperties should be greater or equal zero");
                _minProperties = value;
            }
        }

        public IList<string> Required
        {
            get { return _required ?? (_required = new List<string>()); }
        }

        public bool AllowAdditionalProperties { get; set; }

        public JSchema AdditionalProperties
        {
            get { return _additionalProperties ?? (_additionalProperties = new JSchema()); }
            set { _additionalProperties = value; }
        }

        public IList<JToken> Enum
        {
            get { return _enum ?? (_enum = new List<JToken>()); }
        }

        public IList<JSchema> AllOf
        {
            get { return _allOf ?? (_allOf = new List<JSchema>()); }
        }

        public IList<JSchema> AnyOf
        {
            get { return _anyOf ?? (_anyOf = new List<JSchema>()); }
        }

        public IList<JSchema> OneOf
        {
            get { return _oneOf ?? (_oneOf = new List<JSchema>()); }
        }

        public JSchema Not { get; set; }

        public JSchema AdditionalItems
        {
            get { return _additionalItems ?? (_additionalItems = new JSchema()); }
            set { _additionalItems = value; }
        }

        public bool AllowAdditionalItems { get; set; }

        public IDictionary<string, JSchema> SchemaDependencies
        {
            get { return _schemaDependencies ?? (_schemaDependencies = new Dictionary<string, JSchema>()); }
        }

        public IDictionary<string, IList<string>> PropertyDependencies
        {
            get { return _propertyDependencies ?? (_propertyDependencies = new Dictionary<string, IList<string>>()); }
        }

        public IDictionary<string, JToken> ExtensionData
        {
            get { return _extensionData ?? (_extensionData = new Dictionary<string, JToken>()); }
        }

        #endregion

        public JSchema()
        {
            Schema = new JObject();
            AllowAdditionalProperties = true;
            AllowAdditionalItems = true;
        }

        public override string ToString()
        {
            return Schema.ToString();
        }

        public static JSchema Parse(string json, JSchemaResolver resolver = null)
        {
            if (json == null) throw new ArgumentNullException("json");
            if (String.IsNullOrWhiteSpace(json))
                throw new JSchemaException("invalid json");

            JObject jtoken = JObject.Parse(json);

            JSchemaReader reader = new JSchemaReader();
            return reader.ReadSchema(jtoken, resolver);
        }

    }
}
