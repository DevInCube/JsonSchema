using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace My.Json.Schema.Utilities;

// Instance Equality
// https://json-schema.org/draft/2020-12/json-schema-core.html#section-4.2.2
internal sealed class JTokenEqualityComparer : IEqualityComparer<JsonNode?>
{
    public bool Equals(JsonNode? x, JsonNode? y)
    {
        bool xIsNull = x.IsJsonNull();
        bool yIsNull = y.IsJsonNull();

        if (xIsNull && yIsNull)
        {
            return true;
        }

        if (xIsNull || yIsNull)
        {
            return false;
        }

        JsonValueKind xKind = x!.GetValueKind();
        JsonValueKind yKind = y!.GetValueKind();

        if (xKind == JsonValueKind.True && yKind == JsonValueKind.True)
        {
            return true;
        }

        if (xKind == JsonValueKind.False && yKind == JsonValueKind.False)
        {
            return true;
        }

        if (xKind == JsonValueKind.String && yKind == JsonValueKind.String)
        {
            return x.GetValue<string>().Equals(y.GetValue<string>(), StringComparison.Ordinal);
        }

        if (xKind == JsonValueKind.Number && yKind == JsonValueKind.Number)
        {
            return x.GetValue<double>().Equals(y.GetValue<double>());
        }

        if (xKind == JsonValueKind.Array && yKind == JsonValueKind.Array)
        {
            var arr1 = (JsonArray)x;
            var arr2 = (JsonArray)y;
            return Enumerable.SequenceEqual(arr1, arr2, this);
        }

        if (xKind == JsonValueKind.Object && yKind == JsonValueKind.Object)
        {
            var obj1 = (JsonObject)x;
            var obj2 = (JsonObject)y;
            return Enumerable.SequenceEqual(
                obj1.OrderBy(x => x.Key).Select(x => x.Value),
                obj2.OrderBy(x => x.Key).Select(x => x.Value),
                this);
        }

        return JsonNode.DeepEquals(x, y);
    }

    public int GetHashCode(JsonNode? a)
    {
        if (a.IsJsonNull())
        {
            return 0;
        }

        JsonValueKind aKind = a!.GetValueKind();

        if (aKind == JsonValueKind.True || aKind == JsonValueKind.False)
        {
            return a.GetValue<bool>().GetHashCode();
        }

        if (aKind == JsonValueKind.String)
        {
            return a.GetValue<string>().GetHashCode(StringComparison.Ordinal);
        }

        if (aKind == JsonValueKind.Number)
        {
            return a.GetValue<double>().GetHashCode();
        }

        if (aKind == JsonValueKind.Array)
        {
            var arr = (JsonArray)a;
            var code = new HashCode();
            foreach (var item in arr)
            {
                code.Add(item, this);
            }

            return code.ToHashCode();
        }

        if (aKind == JsonValueKind.Object)
        {
            var obj = (JsonObject)a;
            var code = new HashCode();
            foreach (var item in obj.OrderBy(x => x.Key))
            {
                code.Add(item.Value, this);
            }

            return code.ToHashCode();
        }

        return a.GetHashCode();
    }
}
