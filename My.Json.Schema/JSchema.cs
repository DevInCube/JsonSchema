using My.Json.Schema.Utilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace My.Json.Schema;

public class JSchema
{
    internal JObject Schema;

    #region public properties

    public Uri Id
    {
        get;
        set
        {
            field = value;
            if (!Id.IsAbsoluteUri)
            {
                if (string.IsNullOrWhiteSpace(field.OriginalString)
                    || field.OriginalString.Equals("#", StringComparison.Ordinal))
                {
                    throw new JSchemaException("invalid id : {0}".FormatWith(Id));
                }
            }
        }
    }

    public JSchemaType Type { get; set; }

    public IDictionary<string, JSchema> Properties => field ??= new Dictionary<string, JSchema>();

    public IDictionary<string, JSchema> PatternProperties => field ??= new Dictionary<string, JSchema>();

    public string Title { get; set; }

    public string Description { get; set; }

    public object Default { get; set; }

    public string Format { get; set; }

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

    public bool ExclusiveMaximum { get; set; }

    public bool ExclusiveMinimum { get; set; }

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

    public string Pattern
    {
        get;
        set
        {
            field = value;
            if (!StringHelpers.IsValidRegex(field))
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

    public IList<JToken> Enum => field ??= [];

    public IList<JSchema> AllOf => field ??= [];

    public IList<JSchema> AnyOf => field ??= [];

    public IList<JSchema> OneOf => field ??= [];

    public JSchema Not { get; set; }

    public JSchema AdditionalItems
    {
        get => field ??= new JSchema();
        set;
    }

    public bool AllowAdditionalItems { get; set; }

    public IDictionary<string, JSchema> SchemaDependencies => field ??= new Dictionary<string, JSchema>();

    public IDictionary<string, IList<string>> PropertyDependencies => field ??= new Dictionary<string, IList<string>>();

    public IDictionary<string, JToken> ExtensionData => field ??= new Dictionary<string, JToken>();

    #endregion

    public JSchema()
    {
        Schema = [];
        AllowAdditionalProperties = true;
        AllowAdditionalItems = true;
    }

    public override string ToString()
    {
        return Schema.ToString();
    }

    public static JSchema Parse(string json, JSchemaResolver resolver = null)
    {
        ArgumentNullException.ThrowIfNull(json);

        if (string.IsNullOrWhiteSpace(json))
        {
            throw new JSchemaException("invalid json");
        }

        JObject jtoken = JObject.Parse(json);

        JSchemaReader reader = new();
        return reader.ReadSchema(jtoken, resolver);
    }
}
