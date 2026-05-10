using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Nodes;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaValidationTests_Draft4
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft4);

    // exclusiveMaximum (draft-04: boolean flag)

    [TestMethod]
    public void ExclusiveMaximum_Draft4_AtBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        JsonNode? data = JsonNode.Parse("5");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_BelowBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        JsonNode? data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_AboveBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        JsonNode? data = JsonNode.Parse("6");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_FalseFlagAtBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""maximum"":5,""exclusiveMaximum"":false}");
        JsonNode? data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    // exclusiveMinimum (draft-04: boolean flag)

    [TestMethod]
    public void ExclusiveMinimum_Draft4_AtBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        JsonNode? data = JsonNode.Parse("3");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft4_AboveBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        JsonNode? data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft4_BelowBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        JsonNode? data = JsonNode.Parse("2");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft4_FalseFlagAtBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""minimum"":3,""exclusiveMinimum"":false}");
        JsonNode? data = JsonNode.Parse("3");
        Assert.IsTrue(data.IsValid(schema));
    }

    // id-based $ref resolution (draft-04 "id" keyword)

    [TestMethod]
    public void Ref_Draft4_InlineDeref_UsingIdKeyword_Resolves()
    {
        JSchema schema = Parse(@"{
            ""id"": ""http://example.com/root#"",
            ""definitions"": {
                ""str"": { ""id"": ""#str"", ""type"": ""string"" }
            },
            ""properties"": {
                ""name"": { ""$ref"": ""#str"" }
            }
        }");
        JsonNode? valid = JsonNode.Parse(@"{""name"":""Alice""}");
        JsonNode? invalid = JsonNode.Parse(@"{""name"":42}");
        Assert.IsTrue(valid.IsValid(schema));
        Assert.IsFalse(invalid.IsValid(schema));
    }

    // const keyword treated as extension data in draft-04 (no validation effect)

    [TestMethod]
    public void Const_Draft4_AnyValueIsValid()
    {
        JSchema schema = Parse(@"{""const"":42}");
        JsonNode? data = JsonNode.Parse("99");
        Assert.IsTrue(data.IsValid(schema));
    }
}
