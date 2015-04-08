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
        private Stack<Uri> _scopeStack = new Stack<Uri>();
        private IDictionary<Uri, JSchema> _resolutionScopes;

        public JSchemaReader() 
        {
            _resolutionScopes = new Dictionary<Uri, JSchema>(new UriComparer());
        }

        public JSchema ReadSchema(JObject jObject, JSchemaResolver resolver = null)
        {
            if (jObject == null) throw new ArgumentNullException("jObject");

            if (resolver != null)
                this.resolver = resolver;

            JSchema schema;

            JToken t;
            if (jObject.TryGetValue("$ref", out t))
            {
                if (!t.IsString()) throw new JSchemaException();

                string refStr = t.Value<string>();

                schema = ResolveReference(refStr, jObject);
            }
            else
            {
                schema = Load(jObject);
            }
            /*
            if (schema.Id != null)
            {
                if (!schema.Id.IsAbsoluteUri)
                {
                    string reference = schema.Id.OriginalString;

                    Uri baseUri = _schemaStack.Last().Id;

                    Uri scopeUri = new Uri(baseUri, reference);
                    _inlineReferences.Add(scopeUri, schema);
                }
            }
             */
            return schema;
        }

        private JSchema ResolveReference(string refStr, JObject jObject)
        {
            if (String.IsNullOrWhiteSpace(refStr)) throw new JSchemaException("empty reference");

            if (refStr.Equals("#"))
                return _schemaStack.Last();

            if (refStr.Contains('#'))
            {
                foreach (var scope in _resolutionScopes)
                {
                    Uri relativeUri;

                    Uri baseUri = _schemaStack.Last().Id;

                    if (baseUri != null && baseUri.IsAbsoluteUri)
                        relativeUri = new Uri(baseUri, refStr);
                    else
                        relativeUri = new Uri(refStr, UriKind.RelativeOrAbsolute);

                    Uri scopeUri = scope.Key;

                    if (relativeUri.Equals(scopeUri)
                        && relativeUri.Fragment.Equals(scopeUri.Fragment))
                    {
                        JSchema scopeSchema = scope.Value;
                        return scopeSchema;
                    }
                }

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
                    string unescapedPropName = propName.Replace("~1", "/").Replace("~0", "~").Replace("%25", "%");
                    if (!obj.TryGetValue(unescapedPropName, out propVal))
                    {
                        string inline = "#" + unescapedPropName;
                        /*
                        Uri relativeUri;

                        Uri baseUri = _schemaStack.Last().Id;

                        if (baseUri != null && baseUri.IsAbsoluteUri)
                            relativeUri = new Uri(baseUri, refStr);
                        else
                            relativeUri = new Uri(refStr, UriKind.RelativeOrAbsolute);

                        Uri scopeUri = scope.Key;

                        if (relativeUri.Equals(scopeUri)
                            && relativeUri.Fragment.Equals(scopeUri.Fragment))
                        {
                        */
                       // if (_inlineReferences.ContainsKey(inline))
                            //return _inlineReferences[inline];
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
            jschema.schema = jtoken;

            _schemaStack.Push(jschema);

            foreach (JProperty property in jtoken.Properties())
                ProcessSchemaProperty(jschema, property.Name, property.Value);

            if(_scopeStack.Count > 0)
                _scopeStack.Pop();

            _schemaStack.Pop();
            return jschema;
        }

        private void ProcessSchemaProperty(JSchema jschema, string name, JToken value)
        {
            switch (name)
            {
                case ("id"):
                    {
                        string id = ReadString(value, name);
                        jschema.Id = new Uri(id, UriKind.RelativeOrAbsolute);

                        Uri scopeUri;
                        if (_scopeStack.Count > 0)
                            scopeUri = new Uri(_scopeStack.Peek(), jschema.Id);
                        else
                            scopeUri = jschema.Id;
                        _scopeStack.Push(scopeUri);
                        _resolutionScopes.Add(scopeUri, jschema);

                        break;
                    }
                case ("title"):
                    {
                        jschema.Title = ReadString(value, name);
                        break;
                    }
                case ("description"):
                    {
                        jschema.Description = ReadString(value, name);
                        break;
                    }
                case ("default"):
                    {
                        jschema.Default = value;
                        break;
                    }
                case ("format"):
                    {
                        jschema.Format = ReadString(value, name);
                        break;
                    }
                case ("type"):
                    {
                        if (value.Type == JTokenType.String)
                        {
                            jschema.Type = JSchemaTypeHelpers.ParseType(value.Value<string>());
                        }
                        else if (value.Type == JTokenType.Array)
                        {
                            JEnumerable<JToken> array = value.Value<JArray>().Children();
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
                        else throw new JSchemaException("type is " + value.Type.ToString());
                        break;
                    }
                case ("pattern"):
                    {
                        jschema.Pattern = ReadString(value, name);
                        break;
                    }
                case ("items"):
                    {
                        if (value.Type == JTokenType.Undefined
                            || value.Type == JTokenType.Null)
                        {
                            jschema.ItemsSchema = new JSchema();
                        }
                        else if (value.Type == JTokenType.Object)
                        {
                            JObject obj = value as JObject;
                            jschema.ItemsSchema = ReadSchema(obj);
                        }
                        else if (value.Type == JTokenType.Array)
                        {
                            foreach (var jsh in (value as JArray).Children())
                            {
                                if (jsh.Type != JTokenType.Object) throw new JSchemaException();
                                JObject jobj = jsh as JObject;
                                jschema.ItemsArray.Add(ReadSchema(jobj));
                            }
                        }
                        else throw new JSchemaException("items is " + value.Type.ToString());
                        break;
                    }
                case ("dependencies"):
                    {
                        if (value.Type != JTokenType.Object) throw new JSchemaException();
                        JObject dependencies = value as JObject;

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
                        if (value.Type != JTokenType.Object) throw new JSchemaException();
                        JObject definitions = value as JObject;

                        jschema.ExtensionData["definitions"] = definitions;

                        foreach (JProperty prop in definitions.Properties())
                        {
                            if (prop.Value.Type != JTokenType.Object) throw new JSchemaException();

                            JObject def = prop.Value as JObject;

                            JSchema schema = ReadSchema(def, resolver);
                        }
                        break;
                    }
                case ("properties"):
                    {
                        if (value.Type != JTokenType.Object) throw new JSchemaException();
                        JObject props = value as JObject;
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
                        if (value.Type != JTokenType.Object) throw new JSchemaException();
                        JObject props = value as JObject;
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
                        jschema.MultipleOf = ReadDouble(value, name);
                        break;
                    }
                case ("maximum"):
                    {
                        jschema.Maximum = ReadDouble(value, name);
                        break;
                    }
                case ("minimum"):
                    {
                        jschema.Minimum = ReadDouble(value, name);
                        break;
                    }
                case ("exclusiveMaximum"):
                    {
                        if (jschema.Maximum == null) throw new JSchemaException("maximum not set");
                        jschema.ExclusiveMaximum = ReadBoolean(value, name);
                        break;
                    }
                case ("exclusiveMinimum"):
                    {
                        if (jschema.Minimum == null) throw new JSchemaException("minimum not set");
                        jschema.ExclusiveMinimum = ReadBoolean(value, name);
                        break;
                    }
                case ("maxLength"):
                    {
                        jschema.MaxLength = ReadInteger(value, name);
                        break;
                    }
                case ("minLength"):
                    {
                        jschema.MinLength = ReadInteger(value, name);
                        break;
                    }
                case ("maxItems"):
                    {
                        jschema.MaxItems = ReadInteger(value, name);
                        break;
                    }
                case ("minItems"):
                    {
                        jschema.MinItems = ReadInteger(value, name);
                        break;
                    }
                case ("uniqueItems"):
                    {
                        jschema.UniqueItems = ReadBoolean(value, name);
                        break;
                    }
                case ("maxProperties"):
                    {
                        jschema.MaxProperties = ReadInteger(value, name);
                        break;
                    }
                case ("minProperties"):
                    {
                        jschema.MinProperties = ReadInteger(value, name);
                        break;
                    }
                case ("required"):
                    {
                        if (value.Type != JTokenType.Array) throw new JSchemaException();
                        JArray array = value as JArray;
                        if (value.Count() == 0) throw new JSchemaException();
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
                        if (value.Type != JTokenType.Array) throw new JSchemaException();
                        JArray array = value as JArray;
                        if (value.Count() == 0) throw new JSchemaException();
                        foreach (var enumItem in array.Children())
                        {
                            if (jschema.Enum.Contains(enumItem)) throw new JSchemaException("already contains");
                            jschema.Enum.Add(enumItem);
                        }
                        break;
                    }
                case ("additionalProperties"):
                    {
                        if (!(value.Type == JTokenType.Boolean || value.Type == JTokenType.Object)) throw new JSchemaException();
                        if (value.Type == JTokenType.Boolean)
                        {
                            bool allow = value.Value<bool>();
                            jschema.AllowAdditionalProperties = allow;
                        }
                        else if (value.Type == JTokenType.Object)
                        {
                            JObject obj = value as JObject;
                            jschema.AdditionalProperties = ReadSchema(obj);
                        }
                        break;
                    }
                case ("allOf"):
                    {
                        var schemas = ReadSchemaArray(value, name);
                        foreach (var sh in schemas)
                            jschema.AllOf.Add(sh);
                        break;
                    }
                case ("anyOf"):
                    {
                        var schemas = ReadSchemaArray(value, name);
                        foreach (var sh in schemas)
                            jschema.AnyOf.Add(sh);
                        break;
                    }
                case ("oneOf"):
                    {
                        var schemas = ReadSchemaArray(value, name);           
                        foreach(var sh in schemas)
                            jschema.OneOf.Add(sh);
                        break;
                    }
                case ("not"):
                    {
                        if (value.Type != JTokenType.Object) throw new JSchemaException();
                        JObject obj = value as JObject;
                        jschema.Not = ReadSchema(obj);
                        break;
                    }
                case ("additionalItems"):
                    {
                        if (!(value.Type == JTokenType.Boolean || value.Type == JTokenType.Object)) throw new JSchemaException();
                        if (value.Type == JTokenType.Boolean)
                        {
                            bool allow = value.Value<bool>();
                            jschema.AllowAdditionalItems = allow;
                        }
                        else if (value.Type == JTokenType.Object)
                        {
                            JObject obj = value as JObject;
                            jschema.AdditionalItems = ReadSchema(obj);
                        }
                        break;
                    }
                default:
                    {
                        jschema.ExtensionData[name] = value;
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
