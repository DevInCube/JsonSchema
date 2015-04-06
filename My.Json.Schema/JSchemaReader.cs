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
                    var unescapedPropName = propName.Replace("~1", "/").Replace("~0", "~").Replace("%25", "%");
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

            foreach (JProperty property in jtoken.Properties())
                ProcessSchemaProperty(jschema, property.Name, property.Value);



            _schemaStack.Pop();
            return jschema;
        }

        private void ProcessSchemaProperty(JSchema jschema, string name, JToken t)
        {
            switch (name)
            {
                case ("id"):
                    {
                        jschema.Id = ReadString(t, name);
                        break;
                    }
                case ("title"):
                    {
                        jschema.Title = ReadString(t, name);
                        break;
                    }
                case ("description"):
                    {
                        jschema.Description = ReadString(t, name);
                        break;
                    }
                case ("default"):
                    {
                        jschema.Default = t;
                        break;
                    }
                case ("format"):
                    {
                        jschema.Format = ReadString(t, name);
                        break;
                    }
                case ("type"):
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
                        break;
                    }
                case ("pattern"):
                    {
                        jschema.Pattern = ReadString(t, name);
                        break;
                    }
                case ("items"):
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
                        break;
                    }
                case ("dependencies"):
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
                        break;
                    }
                case ("definitions"):
                    {
                        if (t.Type != JTokenType.Object) throw new JSchemaException();
                        JObject definitions = t as JObject;

                        jschema.ExtensionData["definitions"] = definitions;

                        _inlineReferences = new Dictionary<string, JSchema>();
                        foreach (JProperty prop in definitions.Properties())
                        {
                            if (prop.Value.Type != JTokenType.Object) throw new JSchemaException();

                            JObject def = prop.Value as JObject;

                            JSchema schema = ReadSchema(def, resolver);
                            if (schema.Id != null && schema.Id.StartsWith("#"))
                            {
                                string reference = schema.Id;
                                _inlineReferences.Add(reference, schema);
                            }
                        }
                        break;
                    }
                case ("properties"):
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
                        break;
                    }
                case ("patternProperties"):
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
                        break;
                    }
                case ("multipleOf"):
                    {
                        jschema.MultipleOf = ReadDouble(t, name);
                        break;
                    }
                case ("maximum"):
                    {
                        jschema.Maximum = ReadDouble(t, name);
                        break;
                    }
                case ("minimum"):
                    {
                        jschema.Minimum = ReadDouble(t, name);
                        break;
                    }
                case ("exclusiveMaximum"):
                    {
                        if (jschema.Maximum == null) throw new JSchemaException("maximum not set");
                        jschema.ExclusiveMaximum = ReadBoolean(t, name);
                        break;
                    }
                case ("exclusiveMinimum"):
                    {
                        if (jschema.Minimum == null) throw new JSchemaException("minimum not set");
                        jschema.ExclusiveMinimum = ReadBoolean(t, name);
                        break;
                    }
                case ("maxLength"):
                    {
                        jschema.MaxLength = ReadInteger(t, name);
                        break;
                    }
                case ("minLength"):
                    {
                        jschema.MinLength = ReadInteger(t, name);
                        break;
                    }
                case ("maxItems"):
                    {
                        jschema.MaxItems = ReadInteger(t, name);
                        break;
                    }
                case ("minItems"):
                    {
                        jschema.MinItems = ReadInteger(t, name);
                        break;
                    }
                case ("uniqueItems"):
                    {
                        jschema.UniqueItems = ReadBoolean(t, name);
                        break;
                    }
                case ("maxProperties"):
                    {
                        jschema.MaxProperties = ReadInteger(t, name);
                        break;
                    }
                case ("minProperties"):
                    {
                        jschema.MinProperties = ReadInteger(t, name);
                        break;
                    }
                case ("required"):
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
                        break;
                    }
                case ("enum"):
                    {
                        if (t.Type != JTokenType.Array) throw new JSchemaException();
                        JArray array = t as JArray;
                        if (t.Count() == 0) throw new JSchemaException();
                        foreach (var enumItem in array.Children())
                        {
                            if (jschema.Enum.Contains(enumItem)) throw new JSchemaException("already contains");
                            jschema.Enum.Add(enumItem);
                        }
                        break;
                    }
                case ("additionalProperties"):
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
                        break;
                    }
                case ("allOf"):
                    {
                        var schemas = ReadSchemaArray(t, name);
                        foreach (var sh in schemas)
                            jschema.AllOf.Add(sh);
                        break;
                    }
                case ("anyOf"):
                    {
                        var schemas = ReadSchemaArray(t, name);
                        foreach (var sh in schemas)
                            jschema.AnyOf.Add(sh);
                        break;
                    }
                case ("oneOf"):
                    {
                        var schemas = ReadSchemaArray(t, name);           
                        foreach(var sh in schemas)
                            jschema.OneOf.Add(sh);
                        break;
                    }
                case ("not"):
                    {
                        if (t.Type != JTokenType.Object) throw new JSchemaException();
                        JObject obj = t as JObject;
                        jschema.Not = ReadSchema(obj);
                        break;
                    }
                case ("additionalItems"):
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
                        break;
                    }
                default:
                    {
                        jschema.ExtensionData[name] = t;
                        break;
                    }
            }
        }

        private IList<JSchema> ReadSchemaArray(JToken t, string name)
        {
            if (t.Type != JTokenType.Array) throw new JSchemaException();
            JArray array = t as JArray;
            if (t.Count() == 0) throw new JSchemaException();

            IList<JSchema> list = new List<JSchema>();
            foreach (var item in array.Children())
            {
                if (!(item.Type == JTokenType.Object)) throw new JSchemaException();
                JObject obj = item as JObject;
                list.Add(ReadSchema(obj));
            }
            return list;
        }


        private double? ReadDouble(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                throw new JSchemaException(t.Type.ToString());
            return Convert.ToDouble(t);
        }

        private int? ReadInteger(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Integer))
                throw new JSchemaException(t.Type.ToString());
            return t.Value<int>();
        }

        private bool ReadBoolean(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Boolean))
                throw new JSchemaException(t.Type.ToString());
            return t.Value<bool>();
        }

        private string ReadString(JToken t, string name)
        {
            if (!t.IsString())
                throw new JSchemaException(t.Type.ToString());
            return t.Value<string>();
        }

    }
}
