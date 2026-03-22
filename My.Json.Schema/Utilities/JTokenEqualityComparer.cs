using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace My.Json.Schema.Utilities;

// Instance Equality
// https://json-schema.org/draft/2020-12/json-schema-core.html#section-4.2.2
internal sealed class JTokenEqualityComparer : IEqualityComparer<JToken>
{
    public bool Equals(JToken x, JToken y)
    {
        if (x.Type == JTokenType.Null && y.Type == JTokenType.Null)
        {
            return true;
        }

        if (x.Type == JTokenType.Boolean && y.Type == JTokenType.Boolean)
        {
            return x.Value<bool>().Equals(y.Value<bool>());
        }

        if (x.Type == JTokenType.String && y.Type == JTokenType.String)
        {
            return x.Value<string>().Equals(y.Value<string>(), StringComparison.Ordinal);
        }

        if ((x.Type == JTokenType.Integer || x.Type == JTokenType.Float) &&
            (y.Type == JTokenType.Integer || y.Type == JTokenType.Float))
        {
            return x.Value<double>().Equals(y.Value<double>());
        }

        if (x.Type == JTokenType.Array && y.Type == JTokenType.Array)
        {
            var arr1 = (JArray)x;
            var arr2 = (JArray)y;
            return Enumerable.SequenceEqual(arr1, arr2, this);
        }

        if (x.Type == JTokenType.Object && y.Type == JTokenType.Object)
        {
            var obj1 = (JObject)x;
            var obj2 = (JObject)y;
            return Enumerable.SequenceEqual(
                obj1.Properties().OrderBy(x => x.Name),
                obj2.Properties().OrderBy(x => x.Name),
                this);
        }

        return JToken.DeepEquals(x, y);
    }

    public int GetHashCode([DisallowNull] JToken a)
    {
        if (a.Type == JTokenType.Null)
        {
            return 0;
        }

        if (a.Type == JTokenType.Boolean)
        {
            return a.Value<bool>().GetHashCode();
        }

        if (a.Type == JTokenType.String)
        {
            return a.Value<string>().GetHashCode(StringComparison.Ordinal);
        }

        if ((a.Type == JTokenType.Integer || a.Type == JTokenType.Float))
        {
            return a.Value<double>().GetHashCode();
        }

        if (a.Type == JTokenType.Array)
        {
            var arr1 = (JArray)a;
            var code = new HashCode();
            foreach (var item in arr1)
            {
                code.Add(item, this);
            }

            return code.ToHashCode();
        }

        if (a.Type == JTokenType.Object)
        {
            var obj1 = (JObject)a;
            var code = new HashCode();
            foreach (var item in obj1.Properties().OrderBy(x => x.Name))
            {
                code.Add(item.Value, this);
            }

            return code.ToHashCode();
        }

        return a.GetHashCode();
    }
}
