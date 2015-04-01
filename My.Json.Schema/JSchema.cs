using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using My.Json.Schema.Utilities;

namespace My.Json.Schema
{
    public class JSchema
    {

        private ItemsSchema _items;
        private IDictionary<string, JSchema> _properties;
        public IDictionary<string, JSchema> Properties {
            get
            {
                if (_properties == null)
                    _properties = new Dictionary<string, JSchema>();
                return _properties;
            }
        }

        public static JSchema Parse(string json)
        {
            if (json == null) throw new ArgumentNullException("schema");
            if (String.IsNullOrWhiteSpace(json)) throw new JSchemaException();

            JSchema jschema = new JSchema();
            JObject jtoken = JObject.Parse(json);
            JToken t;
            if (jtoken.TryGetValue("title", out t))
            {
                if (!t.IsString()) 
                    throw new JSchemaException(t.Type.ToString());
                jschema.Title = t.Value<string>();
            }
            if (jtoken.TryGetValue("description", out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(t.Type.ToString());
                jschema.Description = t.Value<string>();
            }
            if (jtoken.TryGetValue("default", out t))
            {
                jschema.Default = t;
            }
            if (jtoken.TryGetValue("format", out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(t.Type.ToString());
                jschema.Format = t.Value<string>();
            }
            if (jtoken.TryGetValue("type", out t))
            {
                if (t.Type == JTokenType.String)
                {
                    switch (t.Value<string>())
                    {
                        case ("null"): jschema.Type = JSchemaType.Null; break;
                        default: throw new JSchemaException();
                    }                    
                }
                else if (t.Type == JTokenType.Array)
                {
                    //@todo
                }
                else throw new JSchemaException("type is " + t.Type.ToString());
            }
            if (jtoken.TryGetValue("items", out t))
            {
                if(t.Type == JTokenType.Undefined
                    || t.Type == JTokenType.Null)
                {
                    jschema._items = new ItemsSchema();
                    jschema._items.Schema = new JSchema();
                } 
                else if (t.Type == JTokenType.Object)
                {
                    jschema._items = new ItemsSchema();
                    jschema._items.Schema = new JSchema();
                    //@todo
                }
                else if (t.Type == JTokenType.Array)
                {
                    jschema._items = new ItemsSchema();
                    jschema._items.Array = new List<JSchema>();
                    //@todo
                }
                else throw new JSchemaException("items is " + t.Type.ToString());
            }
            else
            {
                jschema._items = new ItemsSchema();
                jschema._items.Schema = new JSchema();
            }
            return jschema;
        }

        public JSchema()
        {   
            //
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

        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (!(obj is JSchema)) return false;
            JSchema schema = obj as JSchema;
            return true;    
        }


        public object Type { get; set; }
    }
}
