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

        private JSchemaResolver resolver;
        private JObject schema;
        private ItemsSchema _items;
        private IDictionary<string, JSchema> _properties;
        private IDictionary<string, JSchema> _pattternProperties;
        private double? _multipleOf;
        private int? _maxLength;
        private int? _minLength;
        private int? _maxItems;
        private int? _minItems;
        private int? _maxProperties;
        private int? _minProperties;
        private IList<JToken> _enum;
        private IList<string> _required;

        #region public properties

        public Uri Id { get; set; }
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
        public ItemsSchema Items
        {
            get
            {
                return _items;
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
        public string Pattern { get; set; }
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
        public JSchema AdditionalProperties { get; set; }
        public IList<JToken> Enum
        {
            get
            {
                if (_enum == null)
                    _enum = new List<JToken>();
                return _enum;
            }
        }
        public IList<JSchema> AllOf { get; set; }
        public IList<JSchema> AnyOf { get; set; }
        public IList<JSchema> OneOf { get; set; }
        public JSchema Not { get; set; }

        #endregion

        public JSchema()
        {
            schema = new JObject();            
            AllowAdditionalProperties = true;
        }   

        public override string ToString()
        {
            return schema.ToString();
        }

        public static JSchema Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("schema");
            if (String.IsNullOrWhiteSpace(json)) throw new JSchemaException();

            JObject jtoken = JObject.Parse(json);

            JSchemaReader reader = new JSchemaReader();
            return reader.ReadSchema(jtoken);
        }

        public static JSchema Parse(string json, JSchemaResolver resolver)
        {
            if (json == null) throw new ArgumentNullException("schema");
            if (String.IsNullOrWhiteSpace(json)) throw new JSchemaException();

            JObject jtoken = JObject.Parse(json);

            JSchemaReader reader = new JSchemaReader();
            return reader.ReadSchema(jtoken, resolver);
        }
       

    }
}
