using My.Json.Schema.Utilities;
using System.Text.Json;
using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace My.Json.Schema;

public class JSchemaReader
{
    private readonly Stack<JSchema> _schemaStack = new();
    private readonly Stack<Uri> _scopeStack = new();
    private readonly Dictionary<Uri, JSchema> _resolutionScopes;

    // Pre-scanned index of id-tagged sub-schemas (location-independent identifiers).
    // Populated before any $ref resolution so forward references like "#foo" can be resolved.
    private readonly Dictionary<Uri, JsonObject> _idIndex;

    private JSchemaResolver _resolver;

    public JSchemaReader()
    {
        _resolutionScopes = new Dictionary<Uri, JSchema>(UriComparer.Instance);
        _idIndex = new Dictionary<Uri, JsonObject>(UriComparer.Instance);
    }

    public JSchema ReadSchema(JsonObject jObject, JSchemaResolver inResolver = null)
    {
        ArgumentNullException.ThrowIfNull(jObject);

        if (inResolver != null)
        {
            _resolver = inResolver;
        }

        JSchema schema = Load(jObject);

        if (!jObject.TryGetPropertyValue(SchemaKeywords.Ref, out JsonNode t))
        {
            return schema;
        }

        if (t.GetValueKind() != JsonValueKind.String)
        {
            throw new JSchemaException("$ref should be a string", t);
        }

        string refStr = t.GetValue<string>();

        var resolvedSchema = ResolveReference(refStr, jObject);
        resolvedSchema.Title ??= schema.Title;
        resolvedSchema.Description ??= schema.Description;

        return resolvedSchema;
    }

    private JSchema ResolveReference(string refStr, JsonObject jObject)
    {
        if (string.IsNullOrWhiteSpace(refStr))
        {
            throw new JSchemaException("empty reference", jObject);
        }

        if (refStr.Equals("#"))
        {
            return _schemaStack.Last();
        }

        if (refStr.Contains('#'))
        {
            // Compute the fully-qualified URI the $ref is pointing at (resolved against the
            // current schema stack's id, if any). This is used for both the pre-scanned id
            // index check and the resolution-scope check below.
            Uri resolvedBaseUri = _schemaStack.LastOrDefault()?.Id;
            Uri resolvedRefUri = resolvedBaseUri?.IsAbsoluteUri == true
                ? new Uri(resolvedBaseUri, refStr)
                : new Uri(refStr, UriKind.RelativeOrAbsolute);

            // Location-independent identifier lookup (e.g. `$ref: "#foo"` pointing at a
            // sub-schema previously declared via `id: "#foo"`). Covers the case where the
            // id-tagged sub-schema has not yet been processed through AddScope.
            if (_idIndex.FirstOrDefault(x => UriComparer.Instance.Equals(resolvedRefUri, x.Key)) is var idEntry &&
                idEntry.Key != null)
            {
                return ReadSchema(idEntry.Value, _resolver);
            }

            if (_resolutionScopes.FirstOrDefault(x => UriComparer.Instance.Equals(resolvedRefUri, x.Key)) is var scope &&
                scope.Key != null)
            {
                return scope.Value;
            }

            JsonObject rootObject = (JsonObject)jObject.GetRootParent();
            string[] fragments = refStr.Split('#');
            string fullHost = fragments[0];
            string path = fragments[1];
            if (string.IsNullOrEmpty(fullHost))
            {
                return ResolveInternalReference(fragments[1], rootObject);
            }

            string rootId = null;
            if (rootObject.TryGetPropertyValue(SchemaKeywords.Id, out JsonNode t2))
            {
                rootId = t2.GetValue<string>().Split('#')[0];
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
                if (rootId == null)
                {
                    throw new JSchemaException("missing root id");
                }

                remoteUri = new Uri(new Uri(rootId), refStr);
            }

            return ResolveExternalReference(remoteUri);
        }
        else
        {
            Uri.TryCreate(refStr, UriKind.RelativeOrAbsolute, out Uri refStrUri);

            // Absolute `$ref` may refer directly to a sub-schema declared with the same
            // `id` somewhere in this schema (e.g. `id: "https://.../x.json"` on a
            // `definitions` entry). Check resolution scopes before going external.
            if (refStrUri?.IsAbsoluteUri == true
                && _resolutionScopes.TryGetValue(refStrUri, out JSchema localScope))
            {
                return localScope;
            }

            // Try to resolve internal schema by reference.
            JsonObject rootObject = (JsonObject)jObject.GetRootParent();
            if (rootObject.TryGetPropertyValue(SchemaKeywords.Id, out JsonNode rootIdToken))
            {
                var rootId = rootIdToken.GetValue<string>().Split('#')[0];
                var internalReference = new Uri(CombineUri(rootId, refStr));
                if (_resolutionScopes.TryGetValue(internalReference, out JSchema internalSchema))
                {
                    return internalSchema;
                }
            }

            if (refStrUri?.IsAbsoluteUri == true)
            {
                return ResolveExternalReference(refStrUri);
            }

            if (jObject.TryGetPropertyValue(refStr, out _))
            {
                return ResolveInternalReference(refStr, jObject);
            }

            var parentContainer = jObject.Parent;
            if (parentContainer is JsonArray)
            {
                // e.g. "allOf"
                parentContainer = parentContainer.Parent;
            }

            JsonObject parent = (JsonObject)parentContainer;

            string parentId = parent.TryGetPropertyValue(SchemaKeywords.Id, out JsonNode t2)
                ? t2.GetValue<string>()
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

    // JSON Pointer: https://datatracker.ietf.org/doc/html/rfc6901
    private static JsonObject FindObject(JsonObject rootObject, string jPointer)
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

        static JsonNode GetProperty(JsonNode token, string propName)
        {
            if (token is JsonObject obj)
            {
                string unescapedPropName = UnEscapePropName(propName);
                if (!obj.TryGetPropertyValue(unescapedPropName, out var propVal))
                {
                    throw new JSchemaException($"Missing property '{propName}'.", obj);
                }

                return propVal;
            }

            if (token is JsonArray array)
            {
                if (!int.TryParse(propName, out int index))
                {
                    throw new JSchemaException($"Invalid array index '{propName}'.", token);
                }

                return array[index];
            }

            throw new JSchemaException("property value is not an object or array", token);
        }

        string[] props = !string.IsNullOrEmpty(jPointer)
            ? jPointer.TrimStart('/').Split('/')
            : [];

        JsonNode token = rootObject;
        foreach (string propName in props)
        {
            token = GetProperty(token, propName);
        }

        if (token is not JsonObject tokenObj)
        {
            throw new JSchemaException("ref to non-object", token);
        }

        return tokenObj;
    }

    private JSchema ResolveInternalReference(string jPointer, JsonObject rootObject)
    {
        JsonObject tokenObj = FindObject(rootObject, jPointer);

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

        using var stream = _resolver.GetSchemaResource(newUri);
        using var sr = new StreamReader(stream);
        string jsonContent = sr.ReadToEnd();
        JsonDocumentOptions options = new()
        {
            AllowTrailingCommas = true,
        };
        JsonObject obj = (JsonObject)JsonNode.Parse(jsonContent, documentOptions: options);

        JSchemaReader externalReader = new() { _resolver = _resolver };

        // Pre-index every `id`-tagged sub-schema in the remote file. This must happen BEFORE
        // any $ref resolution so that forward/location-independent references (e.g. `#foo`
        // used before `A: {"id": "#foo"}` is encountered in source order) can be resolved.
        externalReader.PreScanIds(obj);

        // This registers any resolution scopes it declares and builds the root JSchema
        // so that `$ref: "#"` inside the fragment resolves to the remote file's root
        // (not to the fragment sub-schema).
        JSchema externalRootSchema = externalReader.ReadSchema(obj, _resolver);

        string[] fragments = newUri.OriginalString.Split('#');
        string fragment = fragments.Length > 1 ? fragments[1] : null;
        if (string.IsNullOrEmpty(fragment))
        {
            return externalRootSchema;
        }

        // Keep the root on the schema stack while we resolve the fragment so that any
        // `$ref: "#"` encountered inside the fragment's sub-tree sees the remote root.
        externalReader._schemaStack.Push(externalRootSchema);
        try
        {
            return externalReader.ResolveInternalReference(fragment, obj);
        }
        finally
        {
            externalReader._schemaStack.Pop();
        }
    }

    // Locates every `id` property and records the JsonObject it sits on.
    // Used to resolve forward/location-independent `$ref`s.
    private void PreScanIds(JsonObject obj, Uri parentScope = null)
    {
        TryAddId(obj, parentScope);

        foreach (var prop in obj)
        {
            WalkTokenForIds(prop.Value, parentScope);
        }
    }

    private void TryAddId(JsonObject obj, Uri parentScope)
    {
        if (!obj.TryGetPropertyValue(SchemaKeywords.Id, out JsonNode idToken) ||
            idToken.GetValueKind() != JsonValueKind.String)
        {
            return;
        }

        string idStr = idToken.GetValue<string>();
        if (string.IsNullOrEmpty(idStr)
            || !Uri.TryCreate(idStr, UriKind.RelativeOrAbsolute, out Uri idUri))
        {
            return;
        }

        var currentScope = parentScope?.IsAbsoluteUri == true
            ? new Uri(parentScope, idUri)
            : idUri;
        _idIndex[currentScope] = obj;
    }

    private void WalkTokenForIds(JsonNode token, Uri parentScope)
    {
        if (token is JsonObject childObj)
        {
            PreScanIds(childObj, parentScope);
        }
        else if (token is JsonArray childArr)
        {
            foreach (JsonNode item in childArr)
            {
                WalkTokenForIds(item, parentScope);
            }
        }
    }

    private void ReadDefinitions(JsonNode defProp)
    {
        JsonNode value = defProp;
        if (value is not JsonObject definitions)
        {
            throw new JSchemaException("definitions should be an object", value);
        }

        foreach (KeyValuePair<string, JsonNode> prop in definitions)
        {
            if (prop.Value is not JsonObject def)
            {
                throw new JSchemaException("definitions property should be an object", value);
            }

            _ = ReadSchema(def, _resolver);
            // @todo unused schema variable
        }
    }

    private JSchema Load(JsonObject jtoken)
    {
        JSchema jschema = new() { Schema = jtoken };

        _schemaStack.Push(jschema);

        bool popAfter = false;
        if (jtoken.TryGetPropertyValue(SchemaKeywords.Id, out var idProp))
        {
            popAfter = true;
            ProcessSchemaProperty(jschema, SchemaKeywords.Id, idProp);
        }

        if (jtoken.TryGetPropertyValue(SchemaKeywords.Definitions, out var defProp))
        {
            ReadDefinitions(defProp);
        }

        foreach (var property in jtoken.Where(property => !property.Key.Equals(SchemaKeywords.Id)))
        {
            ProcessSchemaProperty(jschema, property.Key, property.Value);
        }

        PostValidate(jschema, jtoken);

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

    private void ProcessSchemaProperty(JSchema jschema, string name, JsonNode value)
    {
        static JSchemaType GetValueKind(JsonNode value)
        {
            static JSchemaType GetArrayType(JsonNode value)
            {
                IEnumerable<JsonNode> array = value.AsArray();
                if (!array.Any())
                {
                    throw new JSchemaException("type array cannot be empty", value);
                }

                JSchemaType result = JSchemaType.None;
                foreach (var arrItem in array)
                {
                    if (arrItem.GetValueKind() != JsonValueKind.String)
                    {
                        throw new JSchemaException("type array items should be strings", arrItem);
                    }

                    JSchemaType parsedType = JSchemaTypeHelpers.ParseType(arrItem.GetValue<string>());
                    if (result == JSchemaType.None)
                    {
                        result = parsedType;
                    }
                    else
                    {
                        if (result.HasFlag(parsedType))
                        {
                            throw new JSchemaException("type array items are not unique", arrItem);
                        }

                        result |= parsedType;
                    }
                }

                return result;
            }

            if (value.GetValueKind() == JsonValueKind.String)
            {
                return JSchemaTypeHelpers.ParseType(value.GetValue<string>());
            }

            if (value is JsonArray)
            {
                return GetArrayType(value);
            }

            throw new JSchemaException("type is " + value.GetValueKind(), value);
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
                    jschema.Type = GetValueKind(value);
                    break;
                }
            case SchemaKeywords.Pattern:
                {
                    jschema.Pattern = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Items:
                {
                    if (value.GetValueKind() == JsonValueKind.Undefined
                        || value.GetValueKind() == JsonValueKind.Null)
                    {
                        jschema.ItemsSchema = new JSchema();
                    }
                    else if (value is JsonObject obj)
                    {
                        jschema.ItemsSchema = ReadSchema(obj, _resolver);
                    }
                    else if (value is JsonArray array)
                    {
                        foreach (var jsh in array)
                        {
                            if (jsh is not JsonObject jobj)
                            {
                                throw new JSchemaException("items elements should be objects", value);
                            }

                            jschema.ItemsArray.Add(ReadSchema(jobj, _resolver));
                        }
                    }
                    else
                    {
                        throw new JSchemaException("items is " + value.GetValueKind(), value);
                    }

                    break;
                }
            case SchemaKeywords.Dependencies:
                {
                    if (value is not JsonObject dependencies)
                    {
                        throw new JSchemaException("dependencies should be an object", value);
                    }

                    foreach (var prop in dependencies)
                    {
                        JsonNode dependency = prop.Value;
                        if (dependency is JsonObject dep)
                        {
                            jschema.SchemaDependencies.Add(prop.Key, ReadSchema(dep, _resolver));
                        }
                        else if (dependency is JsonArray depArray)
                        {
                            if (depArray.Count == 0)
                            {
                                throw new JSchemaException("property dependencies array cannot be empty", depArray);
                            }

                            jschema.PropertyDependencies.Add(prop.Key, []);

                            foreach (var depItem in depArray)
                            {
                                if (depItem.GetValueKind() != JsonValueKind.String)
                                {
                                    throw new JSchemaException("property dependencies array elements should be strings", depItem);
                                }

                                string propName = depItem.GetValue<string>();

                                if (jschema.PropertyDependencies[prop.Key].Contains(propName))
                                {
                                    throw new JSchemaException("property dependencies array elements are not unique", depItem);
                                }

                                jschema.PropertyDependencies[prop.Key].Add(propName);
                            }
                        }
                        else
                        {
                            throw new JSchemaException("dependencies property should be an object or array", dependency);
                        }
                    }

                    break;
                }
            case SchemaKeywords.Properties:
                {
                    if (value is not JsonObject props)
                    {
                        throw new JSchemaException("properties should be an object", value);
                    }

                    foreach (var prop in props)
                    {
                        JsonNode val = prop.Value;
                        if (val is not JsonObject objVal)
                        {
                            throw new JSchemaException("properties property should be an object", val);
                        }

                        jschema.Properties[prop.Key] = ReadSchema(objVal, _resolver);
                    }

                    break;
                }
            case SchemaKeywords.PatternProperties:
                {
                    if (value is not JsonObject props)
                    {
                        throw new JSchemaException("patternProperties should be an object", value);
                    }

                    foreach (var prop in props)
                    {
                        JsonNode val = prop.Value;
                        if (val is not JsonObject objVal)
                        {
                            throw new JSchemaException("patternProperties property should be an object", val);
                        }

                        jschema.PatternProperties[prop.Key] = ReadSchema(objVal, _resolver);
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
                    jschema.ExclusiveMaximum = ReadBoolean(value, name);
                    break;
                }
            case SchemaKeywords.ExclusiveMinimum:
                {
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
                    if (value is not JsonArray array)
                    {
                        throw new JSchemaException("required should be  an array", value);
                    }

                    if (array.Count == 0)
                    {
                        throw new JSchemaException("required array cannot be empty", value);
                    }

                    foreach (var req in array)
                    {
                        if (req.GetValueKind() != JsonValueKind.String)
                        {
                            throw new JSchemaException("required array elements should be strings");
                        }

                        string requiredProp = req.GetValue<string>();
                        if (jschema.Required.Contains(requiredProp))
                        {
                            throw new JSchemaException("already contains", req);
                        }

                        jschema.Required.Add(requiredProp);
                    }

                    break;
                }
            case SchemaKeywords.Enum:
                {
                    if (value is not JsonArray array)
                    {
                        throw new JSchemaException("enum should be an array", value);
                    }

                    if (array.Count == 0)
                    {
                        throw new JSchemaException("enum array cannot be empty", value);
                    }

                    JTokenEqualityComparer comparer = new();
                    foreach (var enumItem in array)
                    {
                        if (jschema.Enum.Contains(enumItem, comparer))
                        {
                            throw new JSchemaException("already contains", enumItem);
                        }

                        jschema.Enum.Add(enumItem);
                    }

                    break;
                }
            case SchemaKeywords.AdditionalProperties:
                {
                    var valueKind = value.GetValueKind();
                    var isBoolean = valueKind == JsonValueKind.True || valueKind == JsonValueKind.False;
                    if (!(isBoolean || value.GetValueKind() == JsonValueKind.Object))
                    {
                        throw new JSchemaException("should not be a boolean or an object");
                    }

                    if (isBoolean)
                    {
                        bool allow = value.GetValue<bool>();
                        jschema.AllowAdditionalProperties = allow;
                    }
                    else if (value is JsonObject obj)
                    {
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
                    if (value is not JsonObject obj)
                    {
                        throw new JSchemaException("should not be an object", value);
                    }

                    jschema.Not = ReadSchema(obj, _resolver);
                    break;
                }
            case SchemaKeywords.AdditionalItems:
                {
                    var valueKind = value.GetValueKind();
                    var isBoolean = valueKind == JsonValueKind.True || valueKind == JsonValueKind.False;
                    if (!(isBoolean || value is JsonObject))
                    {
                        throw new JSchemaException("should not be a boolean or an object");
                    }

                    if (isBoolean)
                    {
                        bool allow = value.GetValue<bool>();
                        jschema.AllowAdditionalItems = allow;
                    }
                    else if (value is JsonObject obj)
                    {
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

    private IEnumerable<JSchema> ReadSchemaArray(JsonNode token, string name)
    {
        if (token is not JsonArray array)
        {
            throw new JSchemaException("{0} should be an array".FormatWith(name), token);
        }

        if (array.Count == 0)
        {
            throw new JSchemaException("{0} array cannot be empty".FormatWith(name), token);
        }

        foreach (var item in array)
        {
            if (item is not JsonObject obj)
            {
                throw new JSchemaException("{0} array items should be objects".FormatWith(name), token);
            }

            yield return ReadSchema(obj, _resolver);
        }
    }

    private static double? ReadDouble(JsonNode token, string name)
    {
        if (token.GetValueKind() != JsonValueKind.Number ||
            !token.AsValue().TryGetValue<double>(out var result))
        {
            throw new JSchemaException("'{0}' : expected number, got {1}".FormatWith(name, token.GetValueKind().ToString()), token);
        }

        return result;
    }

    private static int? ReadInteger(JsonNode token, string name)
    {
        if (token.GetValueKind() != JsonValueKind.Number ||
            !token.AsValue().TryGetValue<long>(out var result))
        {
            throw new JSchemaException("'{0}' : expected integer, got {1}".FormatWith(name, token.GetValueKind().ToString()), token);
        }

        if (result is < int.MinValue or > int.MaxValue)
        {
            throw new JSchemaException("'{0}' : value {1} is out of int range".FormatWith(name, result), token);
        }

        return (int)result;
    }

    private static bool ReadBoolean(JsonNode token, string name)
    {
        if (token.GetValueKind() != JsonValueKind.True && token.GetValueKind() != JsonValueKind.False)
        {
            throw new JSchemaException("'{0}' : expected boolean, got {1}".FormatWith(name, token.GetValueKind().ToString()), token);
        }

        return token.GetValue<bool>();
    }

    private static string ReadString(JsonNode token, string name)
    {
        JsonValueKind kind = token?.GetValueKind() ?? JsonValueKind.Null;
        if (kind == JsonValueKind.Null)
        {
            return null;
        }

        if (kind != JsonValueKind.String)
        {
            throw new JSchemaException("'{0}' : expected string, got {1}".FormatWith(name, kind.ToString()), token);
        }

        return token.GetValue<string>();
    }

    private static void PostValidate(JSchema jschema, JsonObject jtoken)
    {
        if (jtoken.ContainsKey(SchemaKeywords.ExclusiveMaximum) && jschema.Maximum == null)
        {
            throw new JSchemaException("'exclusiveMaximum' requires 'maximum' to be present", jtoken);
        }

        if (jtoken.ContainsKey(SchemaKeywords.ExclusiveMinimum) && jschema.Minimum == null)
        {
            throw new JSchemaException("'exclusiveMinimum' requires 'minimum' to be present", jtoken);
        }
    }
}
