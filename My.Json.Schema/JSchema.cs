using My.Json.Schema.Utilities;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace My.Json.Schema;

public class JSchema
{
    internal JsonObject Schema;

    public bool? IsAlwaysValid { get; internal set; }

    public SchemaVersion Version { get; internal set; } = SchemaVersion.Draft6;

    // Scratch fields: set by JSchemaReader during draft-04 parsing and cleared after normalization.
    internal bool ExclusiveMaximumFlag;
    internal bool ExclusiveMinimumFlag;

    #region public properties

    public Uri? Id
    {
        get;
        set
        {
            field = value;
            if (field != null && !field.IsAbsoluteUri)
            {
                if (string.IsNullOrWhiteSpace(field.OriginalString)
                    || field.OriginalString.Equals("#", StringComparison.Ordinal))
                {
                    throw new JSchemaException("invalid id : {0}".FormatWith(field));
                }
            }
        }
    }

    public JSchemaType Type { get; set; }

    public IDictionary<string, JSchema> Properties => field ??= new Dictionary<string, JSchema>();

    public IDictionary<string, JSchema> PatternProperties => field ??= new Dictionary<string, JSchema>();

    public string? Title { get; set; }

    public string? Description { get; set; }

    public JsonNode? Default { get; set; }

    public string? Format { get; set; }

    public JSchema ItemsSchema
    {
        get => field ??= new JSchema();
        set;
    }

    public IList<JSchema> ItemsArray => field ??= [];

    public double? MultipleOf
    {
        get;
        set
        {
            if (value <= 0)
            {
                throw new JSchemaException("multipleOf should be greater than zero");
            }

            field = value;
        }
    }

    public double? Maximum { get; set; }

    public double? Minimum { get; set; }

    public double? ExclusiveMaximum { get; set; }

    public double? ExclusiveMinimum { get; set; }

    public int? MaxLength
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("maxLength should be greater or equal zero");
            }

            field = value;
        }
    }

    public int? MinLength
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("minLength should be greater or equal zero");
            }

            field = value;
        }
    }

    public string? Pattern
    {
        get;
        set
        {
            field = value;
            if (field != null && !StringHelpers.IsValidRegex(field))
            {
                throw new JSchemaException("pattern is not a valid regex string");
            }
        }
    }

    public int? MaxItems
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("maxItems should be greater or equal zero");
            }

            field = value;
        }
    }

    public int? MinItems
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("minItems should be greater or equal zero");
            }

            field = value;
        }
    }

    public bool UniqueItems { get; set; }

    public int? MaxProperties
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("maxProperties should be greater or equal zero");
            }

            field = value;
        }
    }

    public int? MinProperties
    {
        get;
        set
        {
            if (value < 0)
            {
                throw new JSchemaException("minProperties should be greater or equal zero");
            }

            field = value;
        }
    }

    public IList<string> Required => field ??= [];

    public bool AllowAdditionalProperties { get; set; }

    public JSchema AdditionalProperties
    {
        get => field ??= new JSchema();
        set;
    }

    public IList<JsonNode?> Enum => field ??= [];

    public IList<JSchema> AllOf => field ??= [];

    public IList<JSchema> AnyOf => field ??= [];

    public IList<JSchema> OneOf => field ??= [];

    public JSchema? Not { get; set; }

    public JSchema AdditionalItems
    {
        get => field ??= new JSchema();
        set;
    }

    public bool AllowAdditionalItems { get; set; }

    public IDictionary<string, JSchema> SchemaDependencies => field ??= new Dictionary<string, JSchema>();

    public IDictionary<string, IList<string>> PropertyDependencies => field ??= new Dictionary<string, IList<string>>();

    public IDictionary<string, JsonNode?> ExtensionData => field ??= new Dictionary<string, JsonNode?>();

    public JsonNode? Const { get; set; }

    public JSchema? Contains { get; set; }

    public JSchema? PropertyNames { get; set; }

    #endregion

    public JSchema()
    {
        Schema = [];
        AllowAdditionalProperties = true;
        AllowAdditionalItems = true;
    }

    public override string ToString()
    {
        return Schema?.ToString() ?? (IsAlwaysValid == true ? "true" : "false");
    }

    public static JSchema Parse(string json, JSchemaResolver? resolver = null, SchemaVersion defaultVersion = SchemaVersion.Draft6)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new JSchemaException("invalid json");
        }

        JsonDocumentOptions options = new()
        {
            AllowTrailingCommas = true,
        };
        JsonNode? parsed = JsonNode.Parse(json, documentOptions: options);

        if (parsed is JsonValue val &&
            (val.GetValueKind() == JsonValueKind.True || val.GetValueKind() == JsonValueKind.False))
        {
            if (defaultVersion == SchemaVersion.Draft4)
            {
                throw new JSchemaException("Boolean schemas are not valid in draft-04");
            }

            return new JSchema { IsAlwaysValid = val.GetValue<bool>(), Version = defaultVersion };
        }

        if (parsed is not JsonObject jtoken)
        {
            throw new JSchemaException("schema must be a JSON object or boolean");
        }

        JSchemaReader reader = new(defaultVersion);
        return reader.ReadSchema(jtoken, resolver);
    }
}
