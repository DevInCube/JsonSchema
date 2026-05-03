using System.Text.Json.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;

namespace My.Json.Schema.Utilities;

// Instance Equality
// https://json-schema.org/draft/2020-12/json-schema-core.html#section-4.2.2
internal sealed class JTokenEqualityComparer : IEqualityComparer<JsonNode>
{
    public bool Equals(JsonNode x, JsonNode y)
    {
        JsonValueKind xKind = x?.GetValueKind() ?? JsonValueKind.Null;
        JsonValueKind yKind = y?.GetValueKind() ?? JsonValueKind.Null;
        if (xKind == JsonValueKind.Null && yKind == JsonValueKind.Null)
        {
            return true;
        }

        if (xKind == JsonValueKind.True && yKind == JsonValueKind.True)
        {
            return x.GetValue<bool>().Equals(y.GetValue<bool>());
        }

        if (xKind == JsonValueKind.False && yKind == JsonValueKind.False)
        {
            return x.GetValue<bool>().Equals(y.GetValue<bool>());
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

    public int GetHashCode([DisallowNull] JsonNode a)
    {
        JsonValueKind aKind = a?.GetValueKind() ?? JsonValueKind.Null;
        if (aKind == JsonValueKind.Null)
        {
            return 0;
        }

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
            var arr1 = (JsonArray)a;
            var code = new HashCode();
            foreach (var item in arr1)
            {
                code.Add(item, this);
            }

            return code.ToHashCode();
        }

        if (aKind == JsonValueKind.Object)
        {
            var obj1 = (JsonObject)a;
            var code = new HashCode();
            foreach (var item in obj1.OrderBy(x => x.Key))
            {
                code.Add(item.Value, this);
            }

            return code.ToHashCode();
        }

        return a.GetHashCode();
    }
}
