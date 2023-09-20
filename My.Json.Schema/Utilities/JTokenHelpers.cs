using Newtonsoft.Json.Linq;
using System.Linq;

namespace My.Json.Schema.Utilities
{
    internal static class JTokenHelpers
    {

        public static bool IsString(this JToken t)
        {
            return (t.Type == JTokenType.Undefined
                    || t.Type == JTokenType.Null
                    || t.Type == JTokenType.String);
        }

        public static JToken GetRootParent(this JToken token)
        {
            while (token.Parent != null)
                token = token.Parent;
            return token;
        }

        // Instance Equality
        // https://json-schema.org/draft/2020-12/json-schema-core.html#section-4.2.2
        public static bool IsEqualTo(this JToken a, JToken b)
        {
            if (a.Type == JTokenType.Null && b.Type == JTokenType.Null)
            {
                return true;
            }

            if (a.Type == JTokenType.Boolean && b.Type == JTokenType.Boolean)
            {
                return a.Value<bool>().Equals(b.Value<bool>());
            }

            if (a.Type == JTokenType.String && b.Type == JTokenType.String)
            {
                return a.Value<string>().Equals(b.Value<string>());
            }

            if ((a.Type == JTokenType.Integer || a.Type == JTokenType.Float) &&
                (b.Type == JTokenType.Integer || b.Type == JTokenType.Float))
            {
                return a.Value<double>().Equals(b.Value<double>());
            }

            if (a.Type == JTokenType.Array && b.Type == JTokenType.Array)
            {
                var arr1 = (JArray)a;
                var arr2 = (JArray)b;
                return
                    arr1.Count == arr2.Count &&
                    Enumerable.Range(0, arr1.Count).All(i => arr1[i].IsEqualTo(arr2[i]));
            }

            if (a.Type == JTokenType.Object && b.Type == JTokenType.Object)
            {
                var obj1 = (JObject)a;
                var obj2 = (JObject)b;
                return
                    obj1.Count == obj2.Count &&
                    obj1.Properties().All(p => p.Value.IsEqualTo(obj2[p.Name]));
            }

            return JToken.DeepEquals(a, b);
        }
    }
}
