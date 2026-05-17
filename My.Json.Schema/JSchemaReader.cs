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

    // Pre-scanned index of $id-tagged sub-schemas (location-independent identifiers).
    // Populated before any $ref resolution so forward references like "#foo" can be resolved.
    private readonly Dictionary<Uri, JsonObject> _idIndex;

    private JSchemaResolver? _resolver;

    // The URI this schema was fetched from (set for external readers). Used as base URI for
    // resolving relative $ref values when the schema has no $id of its own.
    private Uri? _baseUri;

    private SchemaVersion _version;

    // Returns the keyword used for the identity/anchor property in the active draft.
    private string IdKeyword => _version == SchemaVersion.Draft4 ? SchemaKeywords.IdDraft4 : SchemaKeywords.Id;

    public JSchemaReader(SchemaVersion defaultVersion = SchemaVersion.Draft6)
    {
        _version = defaultVersion;
        _resolutionScopes = new Dictionary<Uri, JSchema>(UriComparer.Instance);
        _idIndex = new Dictionary<Uri, JsonObject>(UriComparer.Instance);
    }

    public JSchema ReadSchema(JsonObject jObject, JSchemaResolver? inResolver = null)
    {
        ArgumentNullException.ThrowIfNull(jObject);

        if (inResolver != null)
        {
            _resolver = inResolver;
        }

        // Pre-scan all $id declarations in the root document so that forward $ref→$id
        // references (where $ref appears before the sub-schema that declares the $id) resolve
        // correctly. Only done at the top level; recursive calls skip this.
        if (_schemaStack.Count == 0)
        {
            PreScanIds(jObject);
        }

        JSchema schema = Load(jObject);

        if (!jObject.TryGetPropertyValue(SchemaKeywords.Ref, out JsonNode? t))
        {
            return schema;
        }

        if (t?.GetValueKind() != JsonValueKind.String)
        {
            throw new JSchemaException("$ref should be a string", t);
        }

        string refStr = t!.GetValue<string>();

        var resolvedSchema = ResolveReference(refStr, jObject);
        resolvedSchema.Title ??= schema.Title;
        resolvedSchema.Description ??= schema.Description;

        return resolvedSchema;
    }

    private JSchema ReadSchemaNode(JsonNode? value, JSchemaResolver? resolver)
    {
        var kind = value?.GetValueKind() ?? JsonValueKind.Null;
        if (kind == JsonValueKind.True || kind == JsonValueKind.False)
        {
            if (_version == SchemaVersion.Draft4)
            {
                throw new JSchemaException("Boolean schemas are not valid in draft-04", value);
            }

            return new JSchema { IsAlwaysValid = value!.GetValue<bool>(), Version = _version };
        }

        if (value is not JsonObject obj)
        {
            throw new JSchemaException($"schema must be a JSON object or boolean, got {kind}", value);
        }

        return ReadSchema(obj, resolver);
    }

    private JsonObject GetCurrentScopeRoot(JsonObject fallbackDocumentRoot)
    {
        if (_scopeStack.Count > 0
            && _resolutionScopes.TryGetValue(_scopeStack.Peek(), out JSchema? scopeSchema)
            && scopeSchema.Schema != null)
        {
            return scopeSchema.Schema;
        }

        return fallbackDocumentRoot;
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
            // current resolution scope, which may differ from the innermost schema $id when
            // the $id is relative).
            Uri? resolvedBaseUri = _schemaStack.LastOrDefault()?.Id;
            if (resolvedBaseUri == null || !resolvedBaseUri.IsAbsoluteUri)
            {
                resolvedBaseUri = _scopeStack.Count > 0 ? _scopeStack.Peek() : resolvedBaseUri;
            }

            Uri resolvedRefUri = resolvedBaseUri?.IsAbsoluteUri == true
                ? new Uri(resolvedBaseUri, refStr)
                : new Uri(refStr, UriKind.RelativeOrAbsolute);

            // Location-independent identifier lookup (e.g. `$ref: "#foo"` pointing at a
            // sub-schema previously declared via `$id: "#foo"`).
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
                // A JSON Pointer ref starting with '#' is resolved against the root of the
                // current schema resource (the innermost scope with its own $id), not the
                // document root.
                JsonObject scopeRoot = GetCurrentScopeRoot(rootObject);
                return ResolveInternalReference(fragments[1], scopeRoot);
            }

            string? rootId = null;
            if (rootObject.TryGetPropertyValue(IdKeyword, out JsonNode? t2))
            {
                rootId = t2!.GetValue<string>().Split('#')[0];
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
            Uri.TryCreate(refStr, UriKind.RelativeOrAbsolute, out Uri? refStrUri);

            // Absolute `$ref` may refer directly to a sub-schema declared with the same
            // `$id` somewhere in this schema. Check resolution scopes before going external.
            if (refStrUri?.IsAbsoluteUri == true
                && _resolutionScopes.TryGetValue(refStrUri, out JSchema? localScope))
            {
                return localScope;
            }

            // Try to resolve internal schema by reference.
            JsonObject rootObject = (JsonObject)jObject.GetRootParent();
            if (rootObject.TryGetPropertyValue(IdKeyword, out JsonNode? rootIdToken))
            {
                var rootId = rootIdToken!.GetValue<string>().Split('#')[0];
                var internalReference = new Uri(CombineUri(rootId, refStr));
                if (_resolutionScopes.TryGetValue(internalReference, out JSchema? internalSchema))
                {
                    return internalSchema;
                }
            }

            if (refStrUri?.IsAbsoluteUri == true)
            {
                // Check pre-scanned $id index before going external (handles inline schemas
                // whose $id happens to be the same absolute URI as the $ref).
                if (_idIndex.FirstOrDefault(x => UriComparer.Instance.Equals(refStrUri, x.Key)) is var idEntry
                    && idEntry.Key != null)
                {
                    return ReadSchema(idEntry.Value, _resolver);
                }

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

            if (parentContainer == null)
            {
                // Reached the document root without finding the ref. Resolve relative to the
                // base URI the schema was fetched from (for externally-loaded schemas).
                if (_baseUri != null)
                {
                    return ResolveExternalReference(new Uri(_baseUri, refStr));
                }

                throw new JSchemaException($"Cannot resolve relative ref '{refStr}': no base URI available");
            }

            JsonObject parent = (JsonObject)parentContainer;

            string parentId = parent.TryGetPropertyValue(IdKeyword, out JsonNode? t2b)
                ? t2b!.GetValue<string>()
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
    private static JsonNode FindNode(JsonObject rootObject, string jPointer)
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

                return propVal ?? JsonNullSentinel.JsonNull;
            }

            if (token is JsonArray array)
            {
                if (!int.TryParse(propName, out int index))
                {
                    throw new JSchemaException($"Invalid array index '{propName}'.", token);
                }

                return array[index] ?? JsonNullSentinel.JsonNull;
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

        return token;
    }

    private JSchema ResolveInternalReference(string jPointer, JsonObject rootObject)
    {
        JsonNode node = FindNode(rootObject, jPointer);

        if (node?.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
        {
            return new JSchema { IsAlwaysValid = node.GetValue<bool>() };
        }

        if (node is not JsonObject tokenObj)
        {
            throw new JSchemaException("ref to non-object", node);
        }

        // TODO: internal definition schema  with "$ref" : "#" is resolving without root schema in stack.
        return ReadSchema(tokenObj, _resolver);
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
        JsonObject obj = (JsonObject)JsonNode.Parse(jsonContent, documentOptions: options)!;

        // Strip the fragment (if any) to get the document base URI.
        Uri baseUri = newUri.Fragment.Length > 0
            ? new Uri(newUri.GetLeftPart(UriPartial.Path))
            : newUri;

        JSchemaReader externalReader = new(_version) { _resolver = _resolver, _baseUri = baseUri };

        // Pre-index every `$id`-tagged sub-schema in the remote file.
        externalReader.PreScanIds(obj);

        JSchema externalRootSchema = externalReader.ReadSchema(obj, _resolver);

        string[] fragments = newUri.OriginalString.Split('#');
        string? fragment = fragments.Length > 1 ? fragments[1] : null;
        if (string.IsNullOrEmpty(fragment))
        {
            return externalRootSchema;
        }

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

    // Locates every `$id` property and records the JsonObject it sits on.
    private void PreScanIds(JsonObject obj, Uri? parentScope = null)
    {
        Uri? childScope = TryAddId(obj, parentScope);

        foreach (var prop in obj)
        {
            WalkTokenForIds(prop.Value, childScope ?? parentScope);
        }
    }

    private Uri? TryAddId(JsonObject obj, Uri? parentScope)
    {
        if (!obj.TryGetPropertyValue(IdKeyword, out JsonNode? idToken) ||
            idToken?.GetValueKind() != JsonValueKind.String)
        {
            return null;
        }

        string idStr = idToken!.GetValue<string>();
        if (string.IsNullOrEmpty(idStr)
            || !Uri.TryCreate(idStr, UriKind.RelativeOrAbsolute, out Uri? idUri))
        {
            return null;
        }

        var currentScope = parentScope?.IsAbsoluteUri == true
            ? new Uri(parentScope, idUri)
            : idUri;
        _idIndex[currentScope] = obj;
        return currentScope;
    }

    private void WalkTokenForIds(JsonNode? token, Uri? parentScope)
    {
        if (token is JsonObject childObj)
        {
            PreScanIds(childObj, parentScope);
        }
        else if (token is JsonArray childArr)
        {
            foreach (JsonNode? item in childArr)
            {
                WalkTokenForIds(item, parentScope);
            }
        }
    }

    private void ReadDefinitions(JsonNode? defProp)
    {
        if (defProp is not JsonObject definitions)
        {
            throw new JSchemaException("definitions should be an object", defProp);
        }

        foreach (KeyValuePair<string, JsonNode?> prop in definitions)
        {
            if (prop.Value is JsonObject def)
            {
                _ = ReadSchema(def, _resolver);
            }
            else if (prop.Value?.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
            {
                // boolean schema definition is valid.
            }
            else
            {
                throw new JSchemaException("definitions property should be an object or boolean", prop.Value);
            }
        }
    }

    private JSchema Load(JsonObject jtoken)
    {
        JSchema jschema = new() { Schema = jtoken, Version = _version };

        _schemaStack.Push(jschema);

        // Detect draft version from $schema before processing any other keyword.
        if (jtoken.TryGetPropertyValue(SchemaKeywords.Schema, out var schemaProp)
            && schemaProp?.GetValueKind() == System.Text.Json.JsonValueKind.String)
        {
            string schemaUri = schemaProp.GetValue<string>();
            if (schemaUri.Contains("draft-04", StringComparison.OrdinalIgnoreCase))
            {
                _version = SchemaVersion.Draft4;
            }
            else if (schemaUri.Contains("draft-06", StringComparison.OrdinalIgnoreCase))
            {
                _version = SchemaVersion.Draft6;
            }
            else if (schemaUri.Contains("draft-07", StringComparison.OrdinalIgnoreCase))
            {
                _version = SchemaVersion.Draft7;
            }

            jschema.Version = _version;
        }

        bool popAfter = false;
        if (jtoken.TryGetPropertyValue(IdKeyword, out var idProp))
        {
            popAfter = true;
            ProcessSchemaProperty(jschema, IdKeyword, idProp);
        }

        if (jtoken.TryGetPropertyValue(SchemaKeywords.Definitions, out var defProp))
        {
            ReadDefinitions(defProp);
        }

        foreach (var property in jtoken.Where(property => !property.Key.Equals(IdKeyword, StringComparison.Ordinal)))
        {
            ProcessSchemaProperty(jschema, property.Key, property.Value);
        }

        if (_version == SchemaVersion.Draft4)
        {
            NormalizeDraft4ExclusiveBounds(jschema, jtoken);
        }

        if (popAfter && _scopeStack.Count > 0)
        {
            _scopeStack.Pop();
        }

        _schemaStack.Pop();
        return jschema;
    }

    private static void NormalizeDraft4ExclusiveBounds(JSchema jschema, JsonObject jtoken)
    {
        if (jschema.ExclusiveMaximumFlag)
        {
            if (jschema.Maximum == null)
            {
                throw new JSchemaException("'exclusiveMaximum' requires 'maximum' in draft-04", jtoken);
            }

            jschema.ExclusiveMaximum = jschema.Maximum;
            jschema.ExclusiveMaximumFlag = false;
        }

        if (jschema.ExclusiveMinimumFlag)
        {
            if (jschema.Minimum == null)
            {
                throw new JSchemaException("'exclusiveMinimum' requires 'minimum' in draft-04", jtoken);
            }

            jschema.ExclusiveMinimum = jschema.Minimum;
            jschema.ExclusiveMinimumFlag = false;
        }
    }

    private void AddScope(JSchema jschema)
    {
        var scopeUri = _scopeStack.Count > 0
            ? new Uri(_scopeStack.Peek(), jschema.Id!)
            : jschema.Id!;
        _scopeStack.Push(scopeUri);
        _resolutionScopes[scopeUri] = jschema;
    }

    // Called by ProcessSchemaProperty for IdKeyword — routes to the correct keyword string.
    private void ProcessIdKeyword(JSchema jschema, JsonNode? value)
    {
        string id = ReadString(value, IdKeyword)!;
        jschema.Id = new Uri(id, UriKind.RelativeOrAbsolute);
        AddScope(jschema);
    }

    private void ProcessSchemaProperty(JSchema jschema, string name, JsonNode? value)
    {
        static JSchemaType GetValueKind(JsonNode value)
        {
            static JSchemaType GetArrayType(JsonNode value)
            {
                IEnumerable<JsonNode?> array = value.AsArray();
                if (!array.Any())
                {
                    throw new JSchemaException("type array cannot be empty", value);
                }

                JSchemaType result = JSchemaType.None;
                foreach (var arrItem in array)
                {
                    if (arrItem?.GetValueKind() != JsonValueKind.String)
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

        // Check id keyword dynamically so both "id" (draft-04) and "$id" (draft-06) are handled.
        if (name.Equals(IdKeyword, StringComparison.Ordinal))
        {
            ProcessIdKeyword(jschema, value);
            return;
        }

        // Keywords introduced in later drafts are unknown in earlier drafts and must be silently
        // ignored (stored as extension data per the spec's unknown-keyword rule).
        if (_version == SchemaVersion.Draft4
            && (name == SchemaKeywords.Const
                || name == SchemaKeywords.Contains
                || name == SchemaKeywords.PropertyNames))
        {
            jschema.ExtensionData[name] = value;
            return;
        }

        if (_version < SchemaVersion.Draft7
            && (name == SchemaKeywords.If
                || name == SchemaKeywords.Then
                || name == SchemaKeywords.Else
                || name == SchemaKeywords.ContentEncoding
                || name == SchemaKeywords.ContentMediaType))
        {
            jschema.ExtensionData[name] = value;
            return;
        }

        switch (name)
        {
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
                    jschema.Type = GetValueKind(value!);
                    break;
                }
            case SchemaKeywords.Pattern:
                {
                    jschema.Pattern = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.Items:
                {
                    var kind = value?.GetValueKind() ?? JsonValueKind.Null;
                    if (kind == JsonValueKind.True || kind == JsonValueKind.False)
                    {
                        jschema.ItemsSchema = new JSchema { IsAlwaysValid = value!.GetValue<bool>() };
                    }
                    else if (value is JsonObject obj)
                    {
                        jschema.ItemsSchema = ReadSchema(obj, _resolver);
                    }
                    else if (value is JsonArray array)
                    {
                        foreach (var jsh in array)
                        {
                            jschema.ItemsArray.Add(ReadSchemaNode(jsh, _resolver));
                        }
                    }
                    else
                    {
                        throw new JSchemaException($"'items' is {value?.GetValueKind() ?? JsonValueKind.Null}", value);
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
                        JsonNode? dependency = prop.Value;
                        if (dependency is JsonObject dep)
                        {
                            jschema.SchemaDependencies.Add(prop.Key, ReadSchema(dep, _resolver));
                        }
                        else if (dependency is JsonArray depArray)
                        {
                            jschema.PropertyDependencies.Add(prop.Key, []);

                            foreach (var depItem in depArray)
                            {
                                if (depItem?.GetValueKind() != JsonValueKind.String)
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
                        else if (dependency?.GetValueKind() is JsonValueKind.True or JsonValueKind.False)
                        {
                            jschema.SchemaDependencies.Add(prop.Key, new JSchema { IsAlwaysValid = dependency.GetValue<bool>() });
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
                        jschema.Properties[prop.Key] = ReadSchemaNode(prop.Value, _resolver);
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
                        jschema.PatternProperties[prop.Key] = ReadSchemaNode(prop.Value, _resolver);
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
                    if (_version == SchemaVersion.Draft4)
                    {
                        jschema.ExclusiveMaximumFlag = ReadBoolean(value, name);
                    }
                    else
                    {
                        jschema.ExclusiveMaximum = ReadDouble(value, name);
                    }

                    break;
                }
            case SchemaKeywords.ExclusiveMinimum:
                {
                    if (_version == SchemaVersion.Draft4)
                    {
                        jschema.ExclusiveMinimumFlag = ReadBoolean(value, name);
                    }
                    else
                    {
                        jschema.ExclusiveMinimum = ReadDouble(value, name);
                    }

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
                        throw new JSchemaException("'required' must be an array", value);
                    }

                    if (_version == SchemaVersion.Draft4 && array.Count == 0)
                    {
                        throw new JSchemaException(
                            "'required' array must have at least one element in draft-04", value);
                    }

                    foreach (var req in array)
                    {
                        if (req?.GetValueKind() != JsonValueKind.String)
                        {
                            throw new JSchemaException("'required' array elements must be strings", req);
                        }

                        string requiredProp = req.GetValue<string>();
                        if (jschema.Required.Contains(requiredProp))
                        {
                            throw new JSchemaException("'required' array elements must be unique", req);
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
            case SchemaKeywords.Const:
                {
                    jschema.Const = value ?? JsonNullSentinel.JsonNull;
                    break;
                }
            case SchemaKeywords.Contains:
                {
                    jschema.Contains = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.PropertyNames:
                {
                    jschema.PropertyNames = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.AdditionalProperties:
                {
                    var valueKind = value?.GetValueKind() ?? JsonValueKind.Null;
                    var isBoolean = valueKind == JsonValueKind.True || valueKind == JsonValueKind.False;
                    if (isBoolean)
                    {
                        bool allow = value!.GetValue<bool>();
                        jschema.AllowAdditionalProperties = allow;
                    }
                    else if (value is JsonObject obj)
                    {
                        jschema.AdditionalProperties = ReadSchema(obj, _resolver);
                    }
                    else
                    {
                        throw new JSchemaException("'additionalProperties' must be a boolean or an object");
                    }

                    break;
                }
            case SchemaKeywords.AllOf:
                {
                    var schemas = ReadSchemaNodeArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.AllOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.AnyOf:
                {
                    var schemas = ReadSchemaNodeArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.AnyOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.OneOf:
                {
                    var schemas = ReadSchemaNodeArray(value, name);
                    foreach (var sh in schemas)
                    {
                        jschema.OneOf.Add(sh);
                    }

                    break;
                }
            case SchemaKeywords.Not:
                {
                    jschema.Not = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.ContentEncoding:
                {
                    jschema.ContentEncoding = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.ContentMediaType:
                {
                    jschema.ContentMediaType = ReadString(value, name);
                    break;
                }
            case SchemaKeywords.If:
                {
                    jschema.If = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.Then:
                {
                    jschema.Then = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.Else:
                {
                    jschema.Else = ReadSchemaNode(value, _resolver);
                    break;
                }
            case SchemaKeywords.AdditionalItems:
                {
                    var valueKind = value?.GetValueKind() ?? JsonValueKind.Null;
                    var isBoolean = valueKind == JsonValueKind.True || valueKind == JsonValueKind.False;
                    if (isBoolean)
                    {
                        bool allow = value!.GetValue<bool>();
                        jschema.AllowAdditionalItems = allow;
                    }
                    else if (value is JsonObject obj)
                    {
                        jschema.AdditionalItems = ReadSchema(obj, _resolver);
                    }
                    else
                    {
                        throw new JSchemaException("'additionalItems' must be a boolean or an object", value);
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

    private IEnumerable<JSchema> ReadSchemaNodeArray(JsonNode? token, string name)
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
            yield return ReadSchemaNode(item, _resolver);
        }
    }

    private static double? ReadDouble(JsonNode? token, string name)
    {
        if (token?.GetValueKind() != JsonValueKind.Number ||
            !token.AsValue().TryGetValue<double>(out var result))
        {
            throw new JSchemaException("'{0}' : expected number, got {1}".FormatWith(name, token?.GetValueKind() ?? JsonValueKind.Null), token);
        }

        return result;
    }

    private static int? ReadInteger(JsonNode? token, string name)
    {
        if (token?.GetValueKind() != JsonValueKind.Number)
        {
            throw new JSchemaException("'{0}' : expected integer, got {1}".FormatWith(name, token?.GetValueKind() ?? JsonValueKind.Null), token);
        }

        long result;
        if (token.AsValue().TryGetValue<long>(out var longVal))
        {
            result = longVal;
        }
        else if (token.AsValue().TryGetValue<double>(out var doubleVal)
            && !double.IsInfinity(doubleVal)
            && doubleVal == Math.Floor(doubleVal))
        {
            result = (long)doubleVal;
        }
        else
        {
            throw new JSchemaException("'{0}' : expected integer, got Number".FormatWith(name), token);
        }

        if (result is < int.MinValue or > int.MaxValue)
        {
            throw new JSchemaException("'{0}' : value {1} is out of int range".FormatWith(name, result), token);
        }

        return (int)result;
    }

    private static bool ReadBoolean(JsonNode? token, string name)
    {
        if (token?.GetValueKind() is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw new JSchemaException("'{0}' : expected boolean, got {1}".FormatWith(name, token?.GetValueKind() ?? JsonValueKind.Null), token);
        }

        return token.GetValue<bool>();
    }

    private static string? ReadString(JsonNode? token, string name)
    {
        JsonValueKind kind = token?.GetValueKind() ?? JsonValueKind.Null;
        if (kind == JsonValueKind.Null)
        {
            return null;
        }

        if (kind != JsonValueKind.String)
        {
            throw new JSchemaException("'{0}' : expected string, got {1}".FormatWith(name, kind), token);
        }

        return token!.GetValue<string>();
    }
}
