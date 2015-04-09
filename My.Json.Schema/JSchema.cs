using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using My.Json.Schema.Utilities;
using Newtonsoft.Json;
using System.IO;

namespace My.Json.Schema
{
    public class JSchema
    {

        internal JObject schema;

        #region private fields

        private Uri _Id;
        private JSchema _ItemsSchema, _AdditionalItems, _AdditionalProperties;        
        private IDictionary<string, JSchema> _properties, _pattternProperties, _SchemaDependencies;
        private string _Pattern;
        private double? _multipleOf;
        private int? _maxLength, _minLength, _maxItems, _minItems, _maxProperties, _minProperties;
        private IList<JToken> _enum;
        private IList<string> _required;        
        private IList<JSchema> _AllOf, _AnyOf, _OneOf, _ItemsArray;
        private IDictionary<string, IList<string>> _PropertyDependencies;
        private IDictionary<string, JToken> _ExtensionData;

        #endregion

        #region public properties

        public Uri Id
        {
            get { return _Id; }
            set
            {
                _Id = value;
                if (!Id.IsAbsoluteUri)
                {
                    if (String.IsNullOrWhiteSpace(_Id.OriginalString)
                        || _Id.OriginalString.Equals("#"))
                        throw new JSchemaException("invalid id : {0}".FormatWith(Id));
                }
            }
        }
        public JSchemaType Type { get; set; }
        public IDictionary<string, JSchema> Properties {
            get
            {
                if (_properties == null)
                    _properties = new Dictionary<string, JSchema>();
                return _properties;
            }
        }
        public IDictionary<string, JSchema> PatternProperties
        {
            get
            {
                if (_pattternProperties == null)
                    _pattternProperties = new Dictionary<string, JSchema>();
                return _pattternProperties;
            }
        }       
        public string Title { get; set; }
        public string Description { get; set; }
        public object Default { get; set; }
        public string Format { get; set; }
        public JSchema ItemsSchema
        {
            get
            {
                if (_ItemsSchema == null)
                    _ItemsSchema = new JSchema();
                return _ItemsSchema;
            }
            set
            {
                _ItemsSchema = value;
            }
        }
        public IList<JSchema> ItemsArray
        {
            get
            {
                if (_ItemsArray == null)
                    _ItemsArray = new List<JSchema>();
                return _ItemsArray;
            }
        }
        public double? MultipleOf
        {
            get { return _multipleOf; }
            set {
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
            get { return _Pattern; }
            set
            {
                _Pattern = value;
                if (!StringHelpers.IsValidRegex(_Pattern)) 
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
            get
            {
                if (_required == null)
                    _required = new List<string>();
                return _required;
            }
        }
        public bool AllowAdditionalProperties { get; set; }
        public JSchema AdditionalProperties
        {
            get
            {
                if (_AdditionalProperties == null)
                    _AdditionalProperties = new JSchema();
                return _AdditionalProperties;
            }
            set
            {
                _AdditionalProperties = value;
            }
        }
        public IList<JToken> Enum
        {
            get
            {
                if (_enum == null)
                    _enum = new List<JToken>();
                return _enum;
            }
        }
        public IList<JSchema> AllOf
        {
            get
            {
                if (_AllOf == null)
                    _AllOf = new List<JSchema>();
                return _AllOf;
            }
        }
        public IList<JSchema> AnyOf
        {
            get
            {
                if (_AnyOf == null)
                    _AnyOf = new List<JSchema>();
                return _AnyOf;
            }
        }
        public IList<JSchema> OneOf
        {
            get
            {
                if (_OneOf == null)
                    _OneOf = new List<JSchema>();
                return _OneOf;
            }
        }
        public JSchema Not { get; set; }
        public JSchema AdditionalItems
        {
            get
            {
                if (_AdditionalItems == null)
                    _AdditionalItems = new JSchema();
                return _AdditionalItems;
            }
            set
            {
                _AdditionalItems = value;
            }
        }
        public bool AllowAdditionalItems { get; set; }
        public IDictionary<string, JSchema> SchemaDependencies
        {
            get
            {
                if (_SchemaDependencies == null)
                    _SchemaDependencies = new Dictionary<string, JSchema>();
                return _SchemaDependencies;
            }
        }
        public IDictionary<string, IList<string>> PropertyDependencies
        {
            get
            {
                if (_PropertyDependencies == null)
                    _PropertyDependencies = new Dictionary<string, IList<string>>();
                return _PropertyDependencies;
            }
        }
        public IDictionary<string, JToken> ExtensionData
        {
            get
            {
                if (_ExtensionData == null)
                    _ExtensionData = new Dictionary<string, JToken>();
                return _ExtensionData;
            }
        }

        #endregion

        public JSchema()
        {
            schema = new JObject();            
            AllowAdditionalProperties = true;
            AllowAdditionalItems = true;
        }   

        public override string ToString()
        {
            return schema.ToString();
        }

        public static JSchema Parse(string json)
        {
            return Parse(json, null);
        }

        public static JSchema Parse(string json, JSchemaResolver resolver)
        {
            if (json == null) throw new ArgumentNullException("schema");
            if (String.IsNullOrWhiteSpace(json)) 
                throw new JSchemaException("invalid json");

            JObject jtoken = JObject.Parse(json);

            JSchemaReader reader = new JSchemaReader();
            return reader.ReadSchema(jtoken, resolver);
        }

    }
}
