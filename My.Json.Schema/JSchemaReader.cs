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
            _resolutionScopes = new Dictionary<Uri, JSchema>(UriComparer.Instance);
        }

        public JSchema ReadSchema(JObject jObject, JSchemaResolver resolver = null)
        {
            if (jObject == null) throw new ArgumentNullException("jObject");

            if (resolver != null)
                this.resolver = resolver;

            JSchema schema;

            JToken t;
            if (jObject.TryGetValue(SchemaKeywords.Ref, out t))
            {
                if (!t.IsString())
                    throw new JSchemaException(JSchemaException.FormatMessage("$ref should be a string", t.Path, t));

                string refStr = t.Value<string>();

                schema = ResolveReference(refStr, jObject);
            }
            else
            {
                schema = Load(jObject);
            }

            return schema;
        }

        private JSchema ResolveReference(string refStr, JObject jObject)
        {
            if (String.IsNullOrWhiteSpace(refStr))
                throw new JSchemaException(JSchemaException.FormatMessage("empty reference", jObject.Path, jObject));

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

                    if (UriComparer.Instance.Equals(relativeUri, scopeUri))
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
                    if (rootObject.TryGetValue(SchemaKeywords.Id, out t2))
                    {
                        rootId = t2.Value<string>().Split('#')[0];
                        if (rootId.Equals(fullHost))
                            return ResolveInternalReference(path, rootObject);
                    }
                    else
                    {
                        if (String.IsNullOrWhiteSpace(fullHost))
                            throw new JSchemaException(JSchemaException.FormatMessage("host is empty", jObject.Path, jObject));
                    }

                    if (this.resolver == null)
                        throw new JSchemaException(JSchemaException.FormatMessage("can't resolve external reference without resolver", jObject.Path, jObject));
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
                        throw new JSchemaException(JSchemaException.FormatMessage("no property named " + propName, obj.Path, obj));
                    }
                }
                else if (token is JArray)
                {
                    int index;
                    if (!int.TryParse(propName, out index))
                        throw new JSchemaException(JSchemaException.FormatMessage("invalid array index " + propName, token.Path, token)); ;
                    JArray array = token as JArray;
                    propVal = array[index];
                }
                else
                {
                    throw new JSchemaException(JSchemaException.FormatMessage("property value is not an object or array", token.Path, token));
                }
                token = propVal;
            }
            if (!(token is JObject))
                throw new JSchemaException(JSchemaException.FormatMessage("ref to non-object", token.Path, token));
            return ReadSchema(token as JObject);
        }

        internal JSchema ResolveExternalReference(Uri newUri)
        {
            if (resolver == null)
                throw new JSchemaException("can't resolve external schema without resolver");
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

            bool popAfter = false;
            var idProp = jtoken.Property(SchemaKeywords.Id);
            if (idProp != null)
            {
                popAfter = true;
                ProcessSchemaProperty(jschema, idProp.Name, idProp.Value);
            }

            var defProp = jtoken.Property(SchemaKeywords.Definitions);
            if (defProp != null)
            {
                JToken value = defProp.Value;
                if (value.Type != JTokenType.Object)
                    throw new JSchemaException(JSchemaException.FormatMessage("definitions should be an object", value.Path, value));
                JObject definitions = value as JObject;

                foreach (JProperty prop in definitions.Properties())
                {
                    if (prop.Value.Type != JTokenType.Object)
                        throw new JSchemaException(JSchemaException.FormatMessage("definitions property should be an object", value.Path, value));
                    JObject def = prop.Value as JObject;

                    JSchema schema = ReadSchema(def, resolver);
                }
            }

            foreach (JProperty property in jtoken.Properties())
                if (!property.Name.Equals(SchemaKeywords.Id))
                    ProcessSchemaProperty(jschema, property.Name, property.Value);

            if (popAfter && _scopeStack.Count > 0)
                _scopeStack.Pop();

            _schemaStack.Pop();
            return jschema;
        }

        private void ProcessSchemaProperty(JSchema jschema, string name, JToken value)
        {
            switch (name)
            {
                case (SchemaKeywords.Id):
                    {
                        string id = ReadString(value, name);
                        jschema.Id = new Uri(id, UriKind.RelativeOrAbsolute);

                        Uri scopeUri;
                        if (_scopeStack.Count > 0)
                            scopeUri = new Uri(_scopeStack.Peek(), jschema.Id);
                        else
                            scopeUri = jschema.Id;
                        _scopeStack.Push(scopeUri);
                        _resolutionScopes[scopeUri] = jschema;

                        break;
                    }
                case (SchemaKeywords.Title):
                    {
                        jschema.Title = ReadString(value, name);
                        break;
                    }
                case (SchemaKeywords.Description):
                    {
                        jschema.Description = ReadString(value, name);
                        break;
                    }
                case (SchemaKeywords.Default):
                    {
                        jschema.Default = value;
                        break;
                    }
                case (SchemaKeywords.Format):
                    {
                        jschema.Format = ReadString(value, name);
                        break;
                    }
                case (SchemaKeywords.Type):
                    {
                        if (value.Type == JTokenType.String)
                        {
                            jschema.Type = JSchemaTypeHelpers.ParseType(value.Value<string>());
                        }
                        else if (value.Type == JTokenType.Array)
                        {
                            JEnumerable<JToken> array = value.Value<JArray>().Children();
                            if (array.Count() == 0) throw new JSchemaException(JSchemaException.FormatMessage("type array cannot be empty", value.Path, value));
                            foreach (var arrItem in array)
                            {
                                if (arrItem.Type != JTokenType.String)
                                    throw new JSchemaException(JSchemaException.FormatMessage("type array items should be strings", arrItem.Path, arrItem));
                                JSchemaType type = JSchemaTypeHelpers.ParseType(arrItem.Value<string>());
                                if (jschema.Type == JSchemaType.None)
                                    jschema.Type = type;
                                else
                                {
                                    if (jschema.Type.HasFlag(type))
                                        throw new JSchemaException(JSchemaException.FormatMessage("type array items are not unique", arrItem.Path, arrItem));
                                    jschema.Type |= type;
                                }
                            }
                        }
                        else throw new JSchemaException(JSchemaException.FormatMessage("type is " + value.Type.ToString(), value.Path, value));
                        break;
                    }
                case (SchemaKeywords.Pattern):
                    {
                        jschema.Pattern = ReadString(value, name);
                        break;
                    }
                case (SchemaKeywords.Items):
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
                                if (jsh.Type != JTokenType.Object)
                                    throw new JSchemaException(JSchemaException.FormatMessage("items elements should be objects", value.Path, value));
                                JObject jobj = jsh as JObject;
                                jschema.ItemsArray.Add(ReadSchema(jobj));
                            }
                        }
                        else throw new JSchemaException(JSchemaException.FormatMessage("items is " + value.Type.ToString(), value.Path, value));
                        break;
                    }
                case (SchemaKeywords.Dependencies):
                    {
                        if (value.Type != JTokenType.Object)
                            throw new JSchemaException(JSchemaException.FormatMessage("dependencies should be an object", value.Path, value));
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

                                if (dep.Count == 0)
                                    throw new JSchemaException(JSchemaException.FormatMessage("property dependencies array cannot be empty", dep.Path, dep));

                                jschema.PropertyDependencies.Add(prop.Name, new List<string>());

                                foreach (var depItem in dep.Children())
                                {
                                    if (depItem.Type != JTokenType.String)
                                        throw new JSchemaException(JSchemaException.FormatMessage("property dependencies array elements should be strings", depItem.Path, depItem));

                                    string propName = depItem.Value<string>();

                                    if (jschema.PropertyDependencies[prop.Name].Contains(propName))
                                        throw new JSchemaException(JSchemaException.FormatMessage("property dependencies array elements are not unique", depItem.Path, depItem));

                                    jschema.PropertyDependencies[prop.Name].Add(propName);
                                }
                            }
                            else
                                throw new JSchemaException(JSchemaException.FormatMessage("dependencies property should be an object or array", dependency.Path, dependency));
                        }
                        break;
                    }
                case (SchemaKeywords.Properties):
                    {
                        if (value.Type != JTokenType.Object)
                            throw new JSchemaException(JSchemaException.FormatMessage("properties should be an object", value.Path, value));
                        JObject props = value as JObject;
                        foreach (var prop in props.Properties())
                        {
                            JToken val = prop.Value;
                            if (!(val.Type == JTokenType.Object))
                                throw new JSchemaException(JSchemaException.FormatMessage("properties property should be an object", val.Path, val));
                            JObject objVal = val as JObject;
                            jschema.Properties[prop.Name] = ReadSchema(objVal);
                        }
                        break;
                    }
                case (SchemaKeywords.PatternProperties):
                    {
                        if (value.Type != JTokenType.Object)
                            throw new JSchemaException(JSchemaException.FormatMessage("patternProperties should be an object", value.Path, value));
                        JObject props = value as JObject;
                        foreach (var prop in props.Properties())
                        {
                            JToken val = prop.Value;
                            if (!(val.Type == JTokenType.Object))
                                throw new JSchemaException(JSchemaException.FormatMessage("patternProperties property should be an object", val.Path, val));
                            JObject objVal = val as JObject;
                            jschema.PatternProperties[prop.Name] = ReadSchema(objVal);
                        }
                        break;
                    }
                case (SchemaKeywords.MultipleOf):
                    {
                        jschema.MultipleOf = ReadDouble(value, name);
                        break;
                    }
                case (SchemaKeywords.Maximum):
                    {
                        jschema.Maximum = ReadDouble(value, name);
                        break;
                    }
                case (SchemaKeywords.Minimum):
                    {
                        jschema.Minimum = ReadDouble(value, name);
                        break;
                    }
                case (SchemaKeywords.ExclusiveMaximum):
                    {
                        if (jschema.Maximum == null)
                            throw new JSchemaException(JSchemaException.FormatMessage("maximum value was not set", value.Path, value));
                        jschema.ExclusiveMaximum = ReadBoolean(value, name);
                        break;
                    }
                case (SchemaKeywords.ExclusiveMinimum):
                    {
                        if (jschema.Minimum == null)
                            throw new JSchemaException(JSchemaException.FormatMessage("minimum not set", value.Path, value));
                        jschema.ExclusiveMinimum = ReadBoolean(value, name);
                        break;
                    }
                case (SchemaKeywords.MaximumLength):
                    {
                        jschema.MaxLength = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.MinimumLength):
                    {
                        jschema.MinLength = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.MaximumItems):
                    {
                        jschema.MaxItems = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.MinimumItems):
                    {
                        jschema.MinItems = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.UniqueItems):
                    {
                        jschema.UniqueItems = ReadBoolean(value, name);
                        break;
                    }
                case (SchemaKeywords.MaximumProperties):
                    {
                        jschema.MaxProperties = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.MinimumProperties):
                    {
                        jschema.MinProperties = ReadInteger(value, name);
                        break;
                    }
                case (SchemaKeywords.Required):
                    {
                        if (value.Type != JTokenType.Array)
                            throw new JSchemaException(JSchemaException.FormatMessage("required should be  an array", value.Path, value));
                        JArray array = value as JArray;
                        if (value.Count() == 0)
                            throw new JSchemaException(JSchemaException.FormatMessage("required array cannot be empty", value.Path, value));
                        foreach (var req in array.Children())
                        {
                            if (!(req.Type == JTokenType.String))
                                throw new JSchemaException(JSchemaException.FormatMessage("required array elements should be strings", value.Path, value));
                            string requiredProp = req.Value<string>();
                            if (jschema.Required.Contains(requiredProp))
                                throw new JSchemaException(JSchemaException.FormatMessage("already contains", req.Path, req));
                            jschema.Required.Add(requiredProp);
                        }
                        break;
                    }
                case (SchemaKeywords.Enum):
                    {
                        if (value.Type != JTokenType.Array)
                            throw new JSchemaException(JSchemaException.FormatMessage("enum should be an array", value.Path, value));
                        JArray array = value as JArray;
                        if (value.Count() == 0)
                            throw new JSchemaException(JSchemaException.FormatMessage("enum array cannot be empty", value.Path, value));
                        foreach (var enumItem in array.Children())
                        {
                            if (jschema.Enum.Contains(enumItem))
                                throw new JSchemaException(JSchemaException.FormatMessage("already contains", enumItem.Path, enumItem));
                            jschema.Enum.Add(enumItem);
                        }
                        break;
                    }
                case (SchemaKeywords.AdditionalProperties):
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
                case (SchemaKeywords.AllOf):
                    {
                        var schemas = ReadSchemaArray(value, name);
                        foreach (var sh in schemas)
                            jschema.AllOf.Add(sh);
                        break;
                    }
                case (SchemaKeywords.AnyOf):
                    {
                        var schemas = ReadSchemaArray(value, name);
                        foreach (var sh in schemas)
                            jschema.AnyOf.Add(sh);
                        break;
                    }
                case (SchemaKeywords.OneOf):
                    {
                        var schemas = ReadSchemaArray(value, name);
                        foreach (var sh in schemas)
                            jschema.OneOf.Add(sh);
                        break;
                    }
                case (SchemaKeywords.Not):
                    {
                        if (value.Type != JTokenType.Object)
                            throw new JSchemaException(JSchemaException.FormatMessage("not should be an object", value.Path, value));
                        JObject obj = value as JObject;
                        jschema.Not = ReadSchema(obj);
                        break;
                    }
                case (SchemaKeywords.AdditionalItems):
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
            if (t.Type != JTokenType.Array)
                throw new JSchemaException(JSchemaException.FormatMessage("{0} should be an array".FormatWith(name), t.Path, t));
            JArray array = t as JArray;
            if (t.Count() == 0)
                throw new JSchemaException(JSchemaException.FormatMessage("{0} array cannot be empty".FormatWith(name), t.Path, t));

            IList<JSchema> list = new List<JSchema>();
            foreach (var item in array.Children())
            {
                if (!(item.Type == JTokenType.Object))
                    throw new JSchemaException(JSchemaException.FormatMessage("{0} array items should be objects".FormatWith(name), t.Path, t));
                JObject obj = item as JObject;
                list.Add(ReadSchema(obj));
            }
            return list;
        }


        private double? ReadDouble(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Float || t.Type == JTokenType.Integer))
                throw new JSchemaException(JSchemaException.FormatMessage("'{0}' : expected number, got {1}".FormatWith(name, t.Type.ToString()), t.Path, t));
            return Convert.ToDouble(t);
        }

        private int? ReadInteger(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Integer))
                throw new JSchemaException(JSchemaException.FormatMessage("'{0}' : expected number, got {1}".FormatWith(name, t.Type.ToString()), t.Path, t));

            return t.Value<int>();
        }

        private bool ReadBoolean(JToken t, string name)
        {
            if (!(t.Type == JTokenType.Boolean))
                throw new JSchemaException(JSchemaException.FormatMessage("'{0}' : expected number, got {1}".FormatWith(name, t.Type.ToString()), t.Path, t));

            return t.Value<bool>();
        }

        private string ReadString(JToken t, string name)
        {
            if (!t.IsString())
                throw new JSchemaException(JSchemaException.FormatMessage("'{0}' : expected number, got {1}".FormatWith(name, t.Type.ToString()), t.Path, t));

            return t.Value<string>();
        }

    }
}