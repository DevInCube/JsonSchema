namespace My.Json.Schema.Utilities;

public static class JSchemaTypeHelpers
{

    public static JSchemaType ParseType(string strType)
    {
        return strType switch
        {
            "array" => JSchemaType.Array,
            "boolean" => JSchemaType.Boolean,
            "integer" => JSchemaType.Integer,
            "number" => JSchemaType.Number,
            "null" => JSchemaType.Null,
            "object" => JSchemaType.Object,
            "string" => JSchemaType.String,
            _ => throw new JSchemaException(),
        };
    }

}
