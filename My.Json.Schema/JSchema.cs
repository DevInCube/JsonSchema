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
        private JSchema parentSchema;
        private JObject schema;
        private ItemsSchema _items;        
        private IDictionary<string, JSchema> _properties;

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

        public JSchema()
        {
            schema = new JObject();
        }

        public JSchema(JObject jObject)
        {
            this.schema = jObject;
            Load(this.schema);
        }              

        private void Load(JObject jtoken)
        {
            JSchema jschema = this;
            JToken t;
            if (jtoken.TryGetValue("id", out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(t.Type.ToString());
                jschema.Id = new Uri(t.Value<string>());
            }
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
                    jschema.Type = JSchemaTypeHelpers.ParseType(t.Value<string>());
                }
                else if (t.Type == JTokenType.Array)
                {
                    JEnumerable<JToken> array = t.Value<JArray>().Children();
                    if (array.Count() == 0) throw new JSchemaException();
                    foreach (var arrItem in array)
                    {
                        if (arrItem.Type != JTokenType.String)
                            throw new JSchemaException();
                        JSchemaType type = JSchemaTypeHelpers.ParseType(arrItem.Value<string>());
                        if (jschema.Type == JSchemaType.None)
                            jschema.Type = type;
                        else
                            jschema.Type |= type;
                    }
                }
                else throw new JSchemaException("type is " + t.Type.ToString());
            }            

            if (jtoken.TryGetValue("items", out t))
            {
                if (t.Type == JTokenType.Undefined
                    || t.Type == JTokenType.Null)
                {
                    jschema._items = new ItemsSchema(new JSchema());
                }
                else if (t.Type == JTokenType.Object)
                {
                    JObject obj = t as JObject;
                    jschema._items = new ItemsSchema(ParseSchema(obj, jschema));
                }
                else if (t.Type == JTokenType.Array)
                {
                    IList<JSchema> schemas = new List<JSchema>();
                    foreach (var jsh in (t as JArray).Children())
                    {
                        if (jsh.Type != JTokenType.Object) throw new JSchemaException();
                        JObject jobj = jsh as JObject;
                        schemas.Add(ParseSchema(jobj, jschema));
                    }
                    jschema._items = new ItemsSchema(schemas);
                }
                else throw new JSchemaException("items is " + t.Type.ToString());
            }
            else
            {
                jschema._items = new ItemsSchema(new JSchema());
            }
            if (jtoken.TryGetValue("properties", out t))
            {
                if (t.Type != JTokenType.Object) throw new JSchemaException();
                JObject props = t as JObject;
                foreach (var prop in props.Properties())
                {
                    JToken val = prop.Value;
                    if (!(val.Type == JTokenType.Object)) throw new JSchemaException();
                    JObject objVal = val as JObject;
                    jschema.Properties[prop.Name] = ParseSchema(objVal, jschema);
                }
            }            
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

            return ParseSchema(jtoken, null);
        }

        public static JSchema Parse(string json, JSchemaResolver resolver)
        {
            if (json == null) throw new ArgumentNullException("schema");
            if (String.IsNullOrWhiteSpace(json)) throw new JSchemaException();

            JObject jtoken = JObject.Parse(json);

            return ParseSchema(jtoken, null, resolver);
        }

        internal static JSchema ParseSchema(
            JObject jObject, 
            JSchema parentSchema, 
            JSchemaResolver resolver = null)
        {
            if (jObject == null) throw new ArgumentNullException("jObject");

            JToken t;
            if (jObject.TryGetValue("$ref", out t))
            {
                if (!t.IsString()) throw new JSchemaException();
                string refStr = t.Value<string>();

                if (parentSchema == null) throw new JSchemaException("$ref in root schema");

                JSchema rootSchema = parentSchema.GetRootSchema();
                Uri baseUri = rootSchema.Id;

                if (!refStr.StartsWith("#") && baseUri == null) 
                    throw new JSchemaException("root id was not provided");

                Uri idUri;
                try
                {
                    idUri = new Uri(refStr);
                    var host = idUri.Host;
                }
                catch (UriFormatException)
                {
                    idUri = new Uri(baseUri, refStr);
                }

                JSchema refSchema = null;

                if (idUri.Host.Equals(baseUri.Host)
                    && idUri.AbsolutePath.Equals(baseUri.AbsolutePath))
                {
                    refSchema = ResolveInternalReference(idUri, rootSchema);
                }
                else
                {
                    refSchema = ResolveExternalReference(idUri, rootSchema.resolver);
                }
               
                refSchema.parentSchema = parentSchema;
                return refSchema;
            }
            else
            {
                var schema = new JSchema();
                schema.resolver = resolver;
                schema.Load(jObject);
                schema.parentSchema = parentSchema;
                return schema;
            }
        }

        internal static JSchema ResolveInternalReference(Uri newUri, JSchema schema)
        {
            string fragment = newUri.Fragment.Remove(0, 1);
            string[] props = fragment.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            JObject obj = schema.schema;
            foreach(string propName in props)
            {
                JToken propVal;
                if (!obj.TryGetValue(propName, out propVal)) throw new JSchemaException("no property named " + propName);                
                if (propVal.Type == JTokenType.Object) 
                    obj = propVal as JObject;
                else
                    throw new JSchemaException("property value is not an object");
            }
            return new JSchema(obj);
        }

        internal static JSchema ResolveExternalReference(Uri newUri, JSchemaResolver resolver)
        {
            if (resolver == null) throw new JSchemaException("can't resolve external schema");
            JObject obj = JObject.Load(new JsonTextReader(new StreamReader(resolver.GetSchemaResource(newUri))));
            JSchema schema = new JSchema(obj);
            return ResolveInternalReference(newUri, schema);
        }

        public JSchema GetRootSchema()
        {
            if (parentSchema == null)
                return this;
            else 
                return parentSchema.GetRootSchema();
        }
        
    }
}
