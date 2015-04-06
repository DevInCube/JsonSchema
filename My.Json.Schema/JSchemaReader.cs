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
    public class JSchemaReader
    {

        private JSchemaResolver resolver;
        private Stack<JSchema> _schemaStack = new Stack<JSchema>();
        private IDictionary<string, JSchema> _inlineReferences;

        public JSchemaReader() { }

        public JSchema ReadSchema(JObject jObject, JSchemaResolver resolver = null)
        {
            if (jObject == null) throw new ArgumentNullException("jObject");

            if (resolver != null)
                this.resolver = resolver;

            JToken t;
            if (jObject.TryGetValue("$ref", out t))
            {
                if (!t.IsString()) throw new JSchemaException();

                string refStr = t.Value<string>();

                return ResolveReference(refStr, jObject);
            }
            else
            {
                return Load(jObject);
            }
        }

        private JSchema ResolveReference(string refStr, JObject jObject)
        {
            if (String.IsNullOrWhiteSpace(refStr)) throw new JSchemaException("empty reference");

            if (refStr.Equals("#"))
                return _schemaStack.Last();

            if (refStr.Contains('#'))
            {
                JObject rootObject = jObject.GetRootParent() as JObject;
                string[] fragments = refStr.Split('#');
                string fullHost = fragments[0];
                string path = fragments[1];
                if (!String.IsNullOrEmpty(fullHost))
                {
                    string rootId = null;
                    JToken t2;
                    if (rootObject.TryGetValue("id", out t2))
                    {
                        rootId = t2.Value<string>().Split('#')[0];
                        if (rootId.Equals(fullHost))
                            return ResolveInternalReference(path, rootObject);
                    }
                    else
                    {
                        if (String.IsNullOrWhiteSpace(fullHost)) throw new JSchemaException();
                    }
                    if (this.resolver == null) throw new JSchemaException();
                    Uri remoteUri;
                    try
                    {
                        remoteUri = new Uri(refStr);
                    }
                    catch (UriFormatException)
                    {
                        remoteUri = new Uri(new Uri(rootId), refStr);
                    }
                    return ResolveExternalReference(remoteUri);
                }
                else
                {
                    return ResolveInternalReference(fragments[1], rootObject);
                }
            }
            else
            {
                try
                {
                    Uri absUri = new Uri(refStr);
                    if (absUri.IsAbsoluteUri)
                    {
                        return ResolveExternalReference(absUri);
                    }
                }
                catch (UriFormatException) { }

                JToken t2;
                if (jObject.TryGetValue(refStr, out t2))
                {
                    return ResolveInternalReference(refStr, jObject);
                }

                JObject parent = (jObject.Parent is JProperty)
                        ? (jObject.Parent as JProperty).Parent as JObject
                        : jObject.Parent as JObject;
                
                string parentId = "";
                if (parent.TryGetValue("id", out t2))          
                    parentId = t2.Value<string>();

                return ResolveReference(string.Concat(parentId, refStr), parent);          
            }
        }

        internal JSchema ResolveInternalReference(string path, JObject rootObject)
        {
            string[] props = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

            JToken token = rootObject;

            foreach (string propName in props)
            {
                JToken propVal;

                if (token is JObject)
                {
                    JObject obj = token as JObject;
                    var unescapedPropName = propName.Replace("~1", "/").Replace("~0", "~").Replace("%25","%");
                    if (!obj.TryGetValue(unescapedPropName, out propVal))
                    {
                        string inline = "#" + unescapedPropName;
                        if (_inlineReferences.ContainsKey(inline))
                            return _inlineReferences[inline];
                        throw new JSchemaException("no property named " + propName);
                    }
                }
                else if (token is JArray)
                {
                    int index;
                    if (!int.TryParse(propName, out index)) throw new JSchemaException("invalid array index " + propName);
                    JArray array = token as JArray;
                    propVal = array[index];
                }
                else
                {
                    throw new JSchemaException("property value is not an object or array");
                }
                token = propVal;
            }
            if (!(token is JObject)) throw new JSchemaException("ref to non-object");
            return ReadSchema(token as JObject);
        }

        internal JSchema ResolveExternalReference(Uri newUri)
        {
            if (resolver == null) throw new JSchemaException("can't resolve external schema");
            JObject obj = JObject.Load(new JsonTextReader(new StreamReader(resolver.GetSchemaResource(newUri))));

            JSchemaReader externalReader = new JSchemaReader();
            JSchema externalSchema;
            string[] fragments = newUri.OriginalString.Split('#');
            if (fragments.Length > 1)
                externalSchema = externalReader.ResolveInternalReference(fragments[1], obj);
            else
                externalSchema = externalReader.ReadSchema(obj, resolver);            
            return externalSchema;
        }

        private JSchema Load(JObject jtoken)
        {
            JSchema jschema = new JSchema();

            _schemaStack.Push(jschema);

            JToken t;
            if (jtoken.TryGetValue("id", out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(t.Type.ToString());
                jschema.Id = t.Value<string>();
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
                        {
                            if (jschema.Type.HasFlag(type)) throw new JSchemaException();
                            jschema.Type |= type;
                        }
                    }
                }
                else throw new JSchemaException("type is " + t.Type.ToString());
            }
            if (jtoken.TryGetValue("pattern", out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(t.Type.ToString());
                jschema.Pattern = t.Value<string>();
            }

            if (jtoken.TryGetValue("items", out t))
            {
                if (t.Type == JTokenType.Undefined
                    || t.Type == JTokenType.Null)
                {
                    jschema.ItemsSchema = new JSchema();
                }
                else if (t.Type == JTokenType.Object)
                {
                    JObject obj = t as JObject;
                    jschema.ItemsSchema = ReadSchema(obj);
                }
                else if (t.Type == JTokenType.Array)
                {                    
                    foreach (var jsh in (t as JArray).Children())
                    {
                        if (jsh.Type != JTokenType.Object) throw new JSchemaException();
                        JObject jobj = jsh as JObject;
                        jschema.ItemsArray.Add(ReadSchema(jobj));
                    }                    
                }
                else throw new JSchemaException("items is " + t.Type.ToString());
            }
            else
            {
                jschema.ItemsSchema = new JSchema();
            }
            if (jtoken.TryGetValue("dependencies", out t))
            {
                if (t.Type != JTokenType.Object) throw new JSchemaException();
                JObject dependencies = t as JObject;

                foreach (var prop in dependencies.Properties())
                {
                    JToken dependency = prop.Value;
                    if (dependency.Type == JTokenType.Object)
                    {
                        JObject dep = dependency as JObject;
                        jschema.SchemaDependencies.Add(prop.Name, ReadSchema(dep));
                    }
                    else if (dependency.Type == JTokenType.Array)
                    {
                        JArray dep = dependency as JArray;

                        if (dep.Count == 0) throw new JSchemaException();

                        jschema.PropertyDependencies.Add(prop.Name, new List<string>());

                        foreach (var depItem in dep.Children())
                        {
                            if (depItem.Type != JTokenType.String) throw new JSchemaException();

                            string propName = depItem.Value<string>();

                            if (jschema.PropertyDependencies[prop.Name].Contains(propName))
                                throw new JSchemaException();

                            jschema.PropertyDependencies[prop.Name].Add(propName);
                        }
                    }
                    else
                        throw new JSchemaException();
                }
            }
            if (jtoken.TryGetValue("definitions", out t))
            {                 
                if (t.Type != JTokenType.Object) throw new JSchemaException();
                JObject definitions = t as JObject;

                _inlineReferences = new Dictionary<string, JSchema>();
                foreach (JProperty prop in definitions.Properties())
                {
                    if (prop.Value.Type != JTokenType.Object) throw new JSchemaException();

                    JObject def = prop.Value as JObject;
                    
                    JSchema schema = ReadSchema(def, resolver);
                    if(schema.Id!=null && schema.Id.StartsWith("#"))
                    {
                        string reference = schema.Id;
                        _inlineReferences.Add(reference, schema);
                    }
                }
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
                    jschema.Properties[prop.Name] = ReadSchema(objVal);
                }
            }
            if (jtoken.TryGetValue("patternProperties", out t))
            {
                if (t.Type != JTokenType.Object) throw new JSchemaException();
                JObject props = t as JObject;
                foreach (var prop in props.Properties())
                {
                    JToken val = prop.Value;
                    if (!(val.Type == JTokenType.Object)) throw new JSchemaException();
                    JObject objVal = val as JObject;
                    jschema.PatternProperties[prop.Name] = ReadSchema(objVal);
                }
            }
            if (jtoken.TryGetValue("multipleOf", out t))
            {
                if (!(t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MultipleOf = t.Value<double>();
            }
            if (jtoken.TryGetValue("maximum", out t))
            {
                if (!(t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.Maximum = Convert.ToDouble(t);
            }
            if (jtoken.TryGetValue("minimum", out t))
            {
                if (!(t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.Minimum = Convert.ToDouble(t);
            }
            if (jtoken.TryGetValue("exclusiveMaximum", out t))
            {
                if (!(t.Type == JTokenType.Boolean))
                    throw new JSchemaException(t.Type.ToString());
                if (jschema.Maximum == null) throw new JSchemaException("maximum not set");
                jschema.ExclusiveMaximum = t.Value<bool>();
            }
            if (jtoken.TryGetValue("exclusiveMinimum", out t))
            {
                if (!(t.Type == JTokenType.Boolean))
                    throw new JSchemaException(t.Type.ToString());
                if (jschema.Minimum == null) throw new JSchemaException("minimum not set");
                jschema.ExclusiveMinimum = t.Value<bool>();
            }
            if (jtoken.TryGetValue("maxLength", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MaxLength = t.Value<int>();
            }
            if (jtoken.TryGetValue("minLength", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MinLength = t.Value<int>();
            }
            if (jtoken.TryGetValue("maxItems", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MaxItems = t.Value<int>();
            }
            if (jtoken.TryGetValue("minItems", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MinItems = t.Value<int>();
            }
            if (jtoken.TryGetValue("uniqueItems", out t))
            {
                if (!(t.Type == JTokenType.Boolean))
                    throw new JSchemaException(t.Type.ToString());
                jschema.UniqueItems = t.Value<bool>();
            }
            if (jtoken.TryGetValue("maxProperties", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MaxProperties = t.Value<int>();
            }
            if (jtoken.TryGetValue("minProperties", out t))
            {
                if (!(t.Type == JTokenType.Integer))
                    throw new JSchemaException(t.Type.ToString());
                jschema.MinProperties = t.Value<int>();
            }
            if (jtoken.TryGetValue("required", out t))
            {
                if (t.Type != JTokenType.Array) throw new JSchemaException();
                JArray array = t as JArray;
                if (t.Count() == 0) throw new JSchemaException();                
                foreach (var req in array.Children())
                {
                    if (!(req.Type == JTokenType.String)) throw new JSchemaException();
                    string requiredProp = req.Value<string>();
                    if (jschema.Required.Contains(requiredProp)) throw new JSchemaException("already contains");
                    jschema.Required.Add(requiredProp);
                }
            }
            if (jtoken.TryGetValue("enum", out t))
            {
                if (t.Type != JTokenType.Array) throw new JSchemaException();
                JArray array = t as JArray;
                if (t.Count() == 0) throw new JSchemaException();
                foreach (var enumItem in array.Children())
                {
                    if (jschema.Enum.Contains(enumItem)) throw new JSchemaException("already contains");
                    jschema.Enum.Add(enumItem);
                }
            }
            if (jtoken.TryGetValue("additionalProperties", out t))
            {
                if (!(t.Type == JTokenType.Boolean || t.Type == JTokenType.Object)) throw new JSchemaException();
                if (t.Type == JTokenType.Boolean)
                {
                    bool allow = t.Value<bool>();
                    jschema.AllowAdditionalProperties = allow;
                }
                else if (t.Type == JTokenType.Object)
                {
                    JObject obj = t as JObject;
                    jschema.AdditionalProperties = ReadSchema(obj);
                }
            }
            if (jtoken.TryGetValue("allOf", out t))
            {
                if (t.Type != JTokenType.Array) throw new JSchemaException();
                JArray array = t as JArray;
                if (t.Count() == 0) throw new JSchemaException();
                foreach (var item in array.Children())
                {
                    if (!(item.Type == JTokenType.Object)) throw new JSchemaException();
                    JObject obj = item as JObject;
                    jschema.AllOf.Add(ReadSchema(obj));
                }
            }
            if (jtoken.TryGetValue("anyOf", out t))
            {
                if (t.Type != JTokenType.Array) throw new JSchemaException();
                JArray array = t as JArray;
                if (t.Count() == 0) throw new JSchemaException();
                foreach (var item in array.Children())
                {
                    if (!(item.Type == JTokenType.Object)) throw new JSchemaException();
                    JObject obj = item as JObject;
                    jschema.AnyOf.Add(ReadSchema(obj));
                }
            }
            if (jtoken.TryGetValue("oneOf", out t))
            {
                if (t.Type != JTokenType.Array) throw new JSchemaException();
                JArray array = t as JArray;
                if (t.Count() == 0) throw new JSchemaException();
                foreach (var item in array.Children())
                {
                    if (!(item.Type == JTokenType.Object)) throw new JSchemaException();
                    JObject obj = item as JObject;
                    jschema.OneOf.Add(ReadSchema(obj));
                }
            }
            if (jtoken.TryGetValue("not", out t))
            {
                if (t.Type != JTokenType.Object) throw new JSchemaException();
                JObject obj = t as JObject;
                jschema.Not = ReadSchema(obj);
            }
            if (jtoken.TryGetValue("additionalItems", out t))
            {
                if (!(t.Type == JTokenType.Boolean || t.Type == JTokenType.Object)) throw new JSchemaException();
                if (t.Type == JTokenType.Boolean)
                {
                    bool allow = t.Value<bool>();
                    jschema.AllowAdditionalItems = allow;
                }
                else if (t.Type == JTokenType.Object)
                {
                    JObject obj = t as JObject;
                    jschema.AdditionalItems = ReadSchema(obj);
                }
            }

            _schemaStack.Pop();
            return jschema;
        }

    }
}
