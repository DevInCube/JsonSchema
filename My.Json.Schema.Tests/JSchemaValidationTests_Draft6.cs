using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Nodes;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaValidationTests_Draft6
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft6);

    // exclusiveMaximum (draft-06: standalone numeric)

    [TestMethod]
    public void ExclusiveMaximum_Draft6_AtBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""exclusiveMaximum"":5}");
        JsonNode? data = JsonNode.Parse("5");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft6_BelowBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""exclusiveMaximum"":5}");
        JsonNode? data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft6_AboveBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""exclusiveMaximum"":5}");
        JsonNode? data = JsonNode.Parse("6");
        Assert.IsFalse(data.IsValid(schema));
    }

    // exclusiveMinimum (draft-06: standalone numeric)

    [TestMethod]
    public void ExclusiveMinimum_Draft6_AtBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""exclusiveMinimum"":3}");
        JsonNode? data = JsonNode.Parse("3");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft6_AboveBoundary_IsValid()
    {
        JSchema schema = Parse(@"{""exclusiveMinimum"":3}");
        JsonNode? data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft6_BelowBoundary_IsNotValid()
    {
        JSchema schema = Parse(@"{""exclusiveMinimum"":3}");
        JsonNode? data = JsonNode.Parse("2");
        Assert.IsFalse(data.IsValid(schema));
    }

    // Boolean schemas

    [TestMethod]
    public void BooleanSchema_Draft6_True_AcceptsAnything()
    {
        JSchema schema = JSchema.Parse("true", defaultVersion: SchemaVersion.Draft6);
        Assert.IsTrue(JsonNode.Parse("42").IsValid(schema));
        Assert.IsTrue(JsonNode.Parse(@"""hello""").IsValid(schema));
        Assert.IsTrue(JsonNode.Parse("null").IsValid(schema));
    }

    [TestMethod]
    public void BooleanSchema_Draft6_False_RejectsEverything()
    {
        JSchema schema = JSchema.Parse("false", defaultVersion: SchemaVersion.Draft6);
        Assert.IsFalse(JsonNode.Parse("42").IsValid(schema));
        Assert.IsFalse(JsonNode.Parse(@"""hello""").IsValid(schema));
        Assert.IsFalse(JsonNode.Parse("null").IsValid(schema));
    }

    // const keyword (draft-06)

    [TestMethod]
    public void Const_Draft6_NullConst_NullData_IsValid()
    {
        JSchema schema = Parse(@"{""const"":null}");
        Assert.IsTrue(JsonNode.Parse("null").IsValid(schema));
    }

    [TestMethod]
    public void Const_Draft6_ObjectConst_MatchingData_IsValid()
    {
        JSchema schema = Parse(@"{""const"":{""a"":1}}");
        JsonNode? data = JsonNode.Parse(@"{""a"":1}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Const_Draft6_ObjectConst_NonMatchingData_IsNotValid()
    {
        JSchema schema = Parse(@"{""const"":{""a"":1}}");
        JsonNode? data = JsonNode.Parse(@"{""a"":2}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // contains keyword (draft-06)

    [TestMethod]
    public void Contains_Draft6_OneMatch_IsValid()
    {
        JSchema schema = Parse(@"{""contains"":{""const"":1}}");
        JsonNode? data = JsonNode.Parse("[2, 1, 3]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Contains_Draft6_NoMatch_IsNotValid()
    {
        JSchema schema = Parse(@"{""contains"":{""const"":1}}");
        JsonNode? data = JsonNode.Parse("[2, 3, 4]");
        Assert.IsFalse(data.IsValid(schema));
    }

    // propertyNames keyword (draft-06)

    [TestMethod]
    public void PropertyNames_Draft6_AllNamesMatch_IsValid()
    {
        JSchema schema = Parse(@"{""propertyNames"":{""minLength"":2}}");
        JsonNode? data = JsonNode.Parse(@"{""ab"":1,""cd"":2}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void PropertyNames_Draft6_OneNameTooShort_IsNotValid()
    {
        JSchema schema = Parse(@"{""propertyNames"":{""minLength"":2}}");
        JsonNode? data = JsonNode.Parse(@"{""ab"":1,""c"":2}");
        Assert.IsFalse(data.IsValid(schema));
    }
}
