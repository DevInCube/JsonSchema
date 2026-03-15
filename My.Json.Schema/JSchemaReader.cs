using My.Json.Schema.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace My.Json.Schema;

public class JSchemaReader
{
    private readonly Stack<JSchema> _schemaStack = new();
    private readonly Stack<Uri> _scopeStack = new();
    private readonly Dictionary<Uri, JSchema> _resolutionScopes;

    private JSchemaResolver _resolver;

    public JSchemaReader()
    {
        _resolutionScopes = new Dictionary<Uri, JSchema>(UriComparer.Instance);
    }

    public JSchema ReadSchema(JObject jObject, JSchemaResolver inResolver = null)
    {
        ArgumentNullException.ThrowIfNull(jObject);

        if (inResolver != null)
        {
            _resolver = inResolver;
        }

        JSchema schema;

        if (jObject.TryGetValue(SchemaKeywords.Ref, out JToken t))
        {
            if (!t.IsString())
            {
                throw new JSchemaException("$ref should be a string", t.Path, t);
            }

            string refStr = t.Value<string>();

            schema = ResolveReference(refStr, jObject);

            JSchema metaSchema = Load(jObject);
            schema.Title ??= metaSchema.Title;

            schema.Description ??= metaSchema.Description;
        }
        else
        {
            schema = Load(jObject);
        }

        return schema;
    }

    private JSchema ResolveReference(string refStr, JObject jObject)
    {
        if (string.IsNullOrWhiteSpace(refStr))
        {
            throw new JSchemaException("empty reference", jObject.Path, jObject);
        }

        if (refStr.Equals("#"))
        {
            return _schemaStack.Last();
        }

        if (refStr.Contains('#'))
        {
            foreach (var scope in _resolutionScopes)
            {
                Uri baseUri = _schemaStack.Last().Id;

                Uri relativeUri = baseUri != null && baseUri.IsAbsoluteUri
                    ? new Uri(baseUri, refStr)
                    : new Uri(refStr, UriKind.RelativeOrAbsolute);

                Uri scopeUri = scope.Key;

                if (UriComparer.Instance.Equals(relativeUri, scopeUri))
                {
                    JSchema scopeSchema = scope.Value;
                    return scopeSchema;
                }
            }

            JObject rootObject = (JObject)jObject.GetRootParent();
            string[] fragments = refStr.Split('#');
            string fullHost = fragments[0];
            string path = fragments[1];
            if (string.IsNullOrEmpty(fullHost))
            {
                return ResolveInternalReference(fragments[1], rootObject);
            }

            string rootId = null;
            if (rootObject.TryGetValue(SchemaKeywords.Id, out JToken t2))
            {
                rootId = t2.Value<string>().Split('#')[0];
                if (rootId.Equals(fullHost, StringComparison.Ordinal))
                {
                    return ResolveInternalReference(path, rootObject);
                }
            }

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
            // Try to resolve internal schema by reference.
            JObject rootObject = (JObject)jObject.GetRootParent();
            if (rootObject.TryGetValue(SchemaKeywords.Id, out JToken rootIdToken))
            {
                var rootId = rootIdToken.Value<string>().Split('#')[0];
                var internalReference = new Uri(CombineUri(rootId, refStr));
                if (_resolutionScopes.TryGetValue(internalReference, out JSchema internalSchema))
                {
                    return internalSchema;
                }
            }

            Uri refStrUri = null;
            try
            {
                if (Uri.TryCreate(refStr, UriKind.RelativeOrAbsolute, out refStrUri)
                    && refStrUri.IsAbsoluteUri)
                {
                    return ResolveExternalReference(refStrUri);
                }
            }
            catch (UriFormatException)
            {
                // Ignore.
            }

            if (jObject.TryGetValue(refStr, out _))
            {
                return ResolveInternalReference(refStr, jObject);
            }

            var property = jObject.Parent as JProperty;
            var parentContainer = property?.Parent ?? jObject.Parent;
            if (parentContainer.Type == JTokenType.Array)
            {
                // e.g. "allOf"
                parentContainer = parentContainer.Parent.Parent;
            }

            JObject parent = (JObject)parentContainer;

            string parentId = parent.TryGetValue(SchemaKeywords.Id, out JToken t2)
                ? t2.Value<string>()
                : string.Empty;

            return ResolveReference(CombineUri(parentId, refStr), parent);
        }
    }

    private static string CombineUri(string parentId, string refStr)
    {
        if (!Uri.TryCreate(parentId, UriKind.RelativeOrAbsolute, out var parentUri))
        {
            throw new InvalidDataException("invalid URI string");
        }

        if (!parentUri.IsAbsoluteUri)
        {
            return string.Concat(parentId, refStr);
        }

        var internalReference = new Uri(parentUri, refStr);
        return internalReference.OriginalString;
    }

    private static JObject FindObject(string path, JObject rootObject)
    {
        static string UnEscapePropName(string propName)
        {
            string unescapedPropName = propName
                .Replace("~1", "/", StringComparison.Ordinal)
                .Replace("~0", "~", StringComparison.Ordinal)
                .Replace("%25", "%", StringComparison.Ordinal)
                .Replace("%22", "\"", StringComparison.Ordinal);
            return unescapedPropName;
        }

        string[] props = !string.IsNullOrEmpty(path)
            ? path.TrimStart('/').Split('/')
            : [];

        JToken token = rootObject;

        foreach (string propName in props)
        {
            JToken propVal;

            if (token is JObject obj)
            {
                string unescapedPropName = UnEscapePropName(propName);
                if (!obj.TryGetValue(unescapedPropName, out propVal))
                {
                    throw new JSchemaException($"Missing property '{propName}'.", obj.Path, obj);
                }
            }
            else if (token is JArray array)
            {
                if (!int.TryParse(propName, out int index))
                {
                    throw new JSchemaException("invalid array index " + propName, token.Path, token);
                }

                propVal = array[index];
            }
            else
            {
                throw new JSchemaException("property value is not an object or array", token.Path, token);
            }

            token = propVal;
        }

        if (token is not JObject tokenObj)
        {
            throw new JSchemaException("ref to non-object", token.Path, token);
        }

        return tokenObj;
    }

    private JSchema ResolveInternalReference(string path, JObject rootObject)
    {
        JObject tokenObj = FindObject(path, rootObject);

        // TODO: internal definition schema  with "$ref" : "#" is resolving without root schema in stack.
        JSchema internalSchema = ReadSchema(tokenObj, _resolver);
        return internalSchema;
    }

    private JSchema ResolveExternalReference(Uri newUri)
    {
        if (_resolver == null)
        {
            throw new JSchemaException("can't resolve external schema without resolver");
        }

        using JsonTextReader reader = new(new StreamReader(_resolver.GetSchemaResource(newUri)));
        JObject obj = JObject.Load(reader);

        JSchemaReader externalReader = new() { _resolver = _resolver };
        string[] fragments = newUri.OriginalString.Split('#');
        var externalSchema = fragments.Length > 1
            ? externalReader.ResolveInternalReference(fragments[1], obj)
            : externalReader.ReadSchema(obj, _resolver);
        return externalSchema;
    }

    private void ReadDefinitions(JProperty defProp)
    {
        JToken value = defProp.Value;
        if (value.Type != JTokenType.Object)
        {
            throw new JSchemaException("definitions should be an object", value.Path, value);
        }

        JObject definitions = (JObject)value;

        foreach (JProperty prop in definitions.Properties())
        {
            if (prop.Value.Type != JTokenType.Object)
            {
                throw new JSchemaException("definitions property should be an object", value.Path, value);
            }

            JObject def = prop.Value as JObject;
            _ = ReadSchema(def, _resolver);
            // @todo unused schema variable
        }
    }

    private JSchema Load(JObject jtoken)
    {
        JSchema jschema = new() { Schema = jtoken };

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
            ReadDefinitions(defProp);
        }

        foreach (var property in jtoken.Properties().Where(property => !property.Name.Equals(SchemaKeywords.Id)))
        {
            ProcessSchemaProperty(jschema, property.Name, property.Value);
        }

        if (popAfter && _scopeStack.Count > 0)
        {
            _scopeStack.Pop();
        }

        _schemaStack.Pop();
        return jschema;
    }

    private void AddScope(JSchema jschema)
    {
        var scopeUri = _scopeStack.Count > 0
            ? new Uri(_scopeStack.Peek(), jschema.Id)
            : jschema.Id;
        _scopeStack.Push(scopeUri);
        _resolutionScopes[scopeUri] = jschema;
    }

    private void ProcessSchemaProperty(JSchema jschema, string name, JToken value)
    {
        static JSchemaType GetType(JToken value)
        {
            static JSchemaType GetArrayType(JToken value)
            {
                JEnumerable<JToken> array = value.Value<JArray>().Children();
                if (!array.Any())
                {
                    throw new JSchemaException("type array cannot be empty", value.Path, value);
                }

                JSchemaType result = JSchemaType.None;
                foreach (var arrItem in array)
                {
                    if (arrItem.Type != JTokenType.String)
                    {
                        throw new JSchemaException("type array items should be strings", arrItem.Path, arrItem);
                    }

                    JSchemaType parsedType = JSchemaTypeHelpers.ParseType(arrItem.Value<string>());
                    if (result == JSchemaType.None)
                    {
                        result = parsedType;
                    }
                    else
                    {
                        if (result.HasFlag(parsedType))
                        {
                            throw new JSchemaException("type array items are not unique", arrItem.Path, arrItem);
                        }

                        result |= parsedType;
                    }
                }

                return result;
            }

            if (value.Type == JTokenType.String)
            {
                return JSchemaTypeHelpers.ParseType(value.Value<string>());
            }

            if (value.Type == JTokenType.Array)
            {
                return GetArrayType(value);
            }

            throw new JSchemaException("type is " + value.Type, value.Path, value);
        }

        void ReadId()
        {
            string id = ReadString(value, name);
            jschema.Id = new Uri(id, UriKind.RelativeOrAbsolute);
            AddScope(jschema);
        }

        switch (name)
        {
            case SchemaKeywords.Id:
                {
                    ReadId();
                    break;
                }
            case SchemaKeywords.Title:
                {
                    jschema.Title = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Description:
                {
                    jschema.Description = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Default:
                {
                    jschema.Default = value;
                    break;
                }
            case SchemaKeywords.Format:
                {
                    jschema.Format = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Type:
                {
                    jschema.Type = GetType(value);
                    break;
                }
            case SchemaKeywords.Pattern:
                {
                    jschema.Pattern = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Items:
                {
                    if (value.Type == JTokenType.Undefined
                        || value.Type == JTokenType.Null)
                    {
                        jschema.ItemsSchema = new JSchema();
                    }
                    else if (value.Type == JTokenType.Object)
                    {
                        JObject obj = value as JObject;
                        jschema.ItemsSchema = ReadSchema(obj, _resolver);
                    }
                    else if (value.Type == JTokenType.Array)
                    {
                        foreach (var jsh in ((JArray)value).Children())
                        {
                            if (jsh.Type != JTokenType.Object)
                            {
                                throw new JSchemaException("items elements should be objects", value.Path, value);
                            }

                            JObject jobj = jsh as JObject;
                            jschema.ItemsArray.Add(ReadSchema(jobj, _resolver));
                        }
                    }
                    else
                    {
                        throw new JSchemaException("items is " + value.Type, value.Path, value);
                    }

                    break;
                }
            case SchemaKeywords.Dependencies:
                {
                    if (value.Type != JTokenType.Object)
                    {
                        throw new JSchemaException("dependencies should be an object", value.Path, value);
                    }

                    JObject dependencies = (JObject)value;

                    foreach (var prop in dependencies.Properties())
                    {
                        JToken dependency = prop.Value;
                        if (dependency.Type == JTokenType.Object)
                        {
                            JObject dep = dependency as JObject;
                            jschema.SchemaDependencies.Add(prop.Name, ReadSchema(dep, _resolver));
                        }
                        else if (dependency.Type == JTokenType.Array)
                        {
                            JArray depArray = (JArray)dependency;

                            if (depArray.Count == 0)
                            {
                                throw new JSchemaException("property dependencies array cannot be empty", depArray.Path, depArray);
                            }

                            jschema.PropertyDependencies.Add(prop.Name, []);

                            foreach (var depItem in depArray.Children())
                            {
                                if (depItem.Type != JTokenType.String)
                                {
                                    throw new JSchemaException("property dependencies array elements should be strings", depItem.Path, depItem);
                                }

                                string propName = depItem.Value<string>();

                                if (jschema.PropertyDependencies[prop.Name].Contains(propName))
                                {
                                    throw new JSchemaException("property dependencies array elements are not unique", depItem.Path, depItem);
                                }

                                jschema.PropertyDependencies[prop.Name].Add(propName);
                            }
                        }
                        else
                        {
                            throw new JSchemaException("dependencies property should be an object or array", dependency.Path, dependency);
                        }
                    }

                    break;
                }
            case SchemaKeywords.Properties:
                {
                    if (value.Type != JTokenType.Object)
                    {
                        throw new JSchemaException("properties should be an object", value.Path, value);
                    }

                    JObject props = (JObject)value;
                    foreach (var prop in props.Properties())
                    {
                        JToken val = prop.Value;
                        if (val.Type != JTokenType.Object)
                        {
                            throw new JSchemaException("properties property should be an object", val.Path, val);
                        }

                        JObject objVal = val as JObject;
                        jschema.Properties[prop.Name] = ReadSchema(objVal, _resolver);
                    }

                    break;
                }
            case SchemaKeywords.PatternProperties:
                {
                    if (value.Type != JTokenType.Object)
                    {
                        throw new JSchemaException("patternProperties should be an object", value.Path, value);
                    }

                    JObject props = value as JObject;
                    foreach (var prop in props.Properties())
                    {
                        JToken val = prop.Value;
                        if (val.Type != JTokenType.Object)
                        {
                            throw new JSchemaException("patternProperties property should be an object", val.Path, val);
                        }

                        JObject objVal = val as JObject;
                        jschema.PatternProperties[prop.Name] = ReadSchema(objVal, _resolver);
                    }

                    break;
                }
            case SchemaKeywords.MultipleOf:
                {
                    jschema.MultipleOf = ReadDouble(value, name);
                    break;
                }
            case SchemaKeywords.Maximum:
                {
                    jschema.Maximum = ReadDouble(value, name);
                    break;
                }
            case SchemaKeywords.Minimum:
                {
                    jschema.Minimum = ReadDouble(value, name);
                    break;
                }
            case SchemaKeywords.ExclusiveMaximum:
                {
                    if (jschema.Maximum == null)
                    {
                        throw new JSchemaException("maximum value was not set", value.Path, value);
                    }

                    jschema.ExclusiveMaximum = ReadBoolean(value, name);
                    break;
                }
            case SchemaKeywords.ExclusiveMinimum:
                {
                    if (jschema.Minimum == null)
                    {
                        throw new JSchemaException("minimum is not set", value.Path, value);
                    }

                    jschema.ExclusiveMinimum = ReadBoolean(value, name);
                    break;
                }
            case SchemaKeywords.MaximumLength:
                {
                    jschema.MaxLength = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.MinimumLength:
                {
                    jschema.MinLength = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.MaximumItems:
                {
                    jschema.MaxItems = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.MinimumItems:
                {
                    jschema.MinItems = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.UniqueItems:
                {
                    jschema.UniqueItems = ReadBoolean(value, name);
                    break;
                }
            case SchemaKeywords.MaximumProperties:
                {
                    jschema.MaxProperties = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.MinimumProperties:
                {
                    jschema.MinProperties = ReadInteger(value, name);
                    break;
                }
            case SchemaKeywords.Required:
                {
                    if (value.Type != JTokenType.Array)
                    {
                        throw new JSchemaException("required should be  an array", value.Path, value);
                    }

                    JArray array = (JArray)value;
                    if (!value.Any())
                    {
                        throw new JSchemaException("required array cannot be empty", value.Path, value);
                    }

                    foreach (var req in array.Children())
                    {
                        if (req.Type != JTokenType.String)
                        {
                            throw new JSchemaException("required array elements should be strings", value.Path, value);
                        }

                        string requiredProp = req.Value<string>();
                        if (jschema.Required.Contains(requiredProp))
                        {
                            throw new JSchemaException("already contains", req.Path, req);
                        }

                        jschema.Required.Add(requiredProp);
                    }

                    break;
                }
            case SchemaKeywords.Enum:
                {
                    if (value.Type != JTokenType.Array)
                    {
                        throw new JSchemaException("enum should be an array", value.Path, value);
                    }

                    JArray array = (JArray)value;
                    if (!value.Any())
                    {
                        throw new JSchemaException("enum array cannot be empty", value.Path, value);
                    }

                    foreach (var enumItem in array.Children())
                    {
                        if (jschema.Enum.Contains(enumItem))
                        {
                            throw new JSchemaException("already contains", enumItem.Path, enumItem);
                        }

                        jschema.Enum.Add(enumItem);
                    }

                    break;
                }
            case SchemaKeywords.AdditionalProperties:
                {
                    if (!(value.Type == JTokenType.Boolean || value.Type == JTokenType.Object))
                    {
                        throw new JSchemaException();
                    }

                    if (value.Type == JTokenType.Boolean)
                    {
                        bool allow = value.Value<bool>();
                        jschema.AllowAdditionalProperties = allow;
                    }
                    else if (value.Type == JTokenType.Object)
                    {
                        JObject obj = value as JObject;
                        jschema.AdditionalProperties = ReadSchema(obj, _resolver);
                    }

                    break;
                }
            case SchemaKeywords.AllOf:
                {
                    var schemas = ReadSchemaArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.AllOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.AnyOf:
                {
                    var schemas = ReadSchemaArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.AnyOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.OneOf:
                {
                    var schemas = ReadSchemaArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.OneOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.Not:
                {
                    if (value.Type != JTokenType.Object)
                    {
                        throw new JSchemaException("should not be an object", value.Path, value);
                    }

                    JObject obj = value as JObject;
                    jschema.Not = ReadSchema(obj, _resolver);
                    break;
                }
            case SchemaKeywords.AdditionalItems:
                {
                    if (!(value.Type == JTokenType.Boolean || value.Type == JTokenType.Object))
                    {
                        throw new JSchemaException("should not be a boolean or an object");
                    }

                    if (value.Type == JTokenType.Boolean)
                    {
                        bool allow = value.Value<bool>();
                        jschema.AllowAdditionalItems = allow;
                    }
                    else if (value.Type == JTokenType.Object)
                    {
                        JObject obj = value as JObject;
                        jschema.AdditionalItems = ReadSchema(obj, _resolver);
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

    private IEnumerable<JSchema> ReadSchemaArray(JToken token, string name)
    {
        if (token.Type != JTokenType.Array)
        {
            throw new JSchemaException("{0} should be an array".FormatWith(name), token.Path, token);
        }

        JArray array = (JArray)token;
        if (!token.Any())
        {
            throw new JSchemaException("{0} array cannot be empty".FormatWith(name), token.Path, token);
        }

        foreach (var item in array.Children())
        {
            if (item.Type != JTokenType.Object)
            {
                throw new JSchemaException("{0} array items should be objects".FormatWith(name), token.Path, token);
            }

            JObject obj = item as JObject;
            yield return ReadSchema(obj);
        }
    }

    private static double? ReadDouble(JToken token, string name)
    {
        if (!(token.Type == JTokenType.Float || token.Type == JTokenType.Integer))
        {
            throw new JSchemaException("'{0}' : expected number, got {1}".FormatWith(name, token.Type.ToString()), token.Path, token);
        }

        return Convert.ToDouble(token, CultureInfo.InvariantCulture);
    }

    private static int? ReadInteger(JToken token, string name)
    {
        if (token.Type != JTokenType.Integer)
        {
            throw new JSchemaException("'{0}' : expected integer, got {1}".FormatWith(name, token.Type.ToString()), token.Path, token);
        }

        return token.Value<int>();
    }

    private static bool ReadBoolean(JToken token, string name)
    {
        if (token.Type != JTokenType.Boolean)
        {
            throw new JSchemaException("'{0}' : expected boolean, got {1}".FormatWith(name, token.Type.ToString()), token.Path, token);
        }

        return token.Value<bool>();
    }

    private static string ReadString(JToken token, string name)
    {
        if (!token.IsString())
        {
            throw new JSchemaException("'{0}' : expected string, got {1}".FormatWith(name, token.Type.ToString()), token.Path, token);
        }

        return token.Value<string>();
    }
}