using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Nodes;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaValidationTests_Draft7
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft7);

    // if / then

    [TestMethod]
    public void IfThen_Draft7_DataMatchesIf_ThenApplied_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""then"":{""minimum"":0}}");
        JsonNode? data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void IfThen_Draft7_DataMatchesIf_ThenApplied_Invalid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""then"":{""minimum"":0}}");
        JsonNode? data = JsonNode.Parse("-1");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void IfThen_Draft7_DataDoesNotMatchIf_ThenIgnored_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""then"":{""minimum"":0}}");
        JsonNode? data = JsonNode.Parse(@"""hello""");
        Assert.IsTrue(data.IsValid(schema));
    }

    // if / else

    [TestMethod]
    public void IfElse_Draft7_DataDoesNotMatchIf_ElseApplied_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""else"":{""maxLength"":5}}");
        JsonNode? data = JsonNode.Parse(@"""hello""");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void IfElse_Draft7_DataDoesNotMatchIf_ElseApplied_Invalid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""else"":{""maxLength"":3}}");
        JsonNode? data = JsonNode.Parse(@"""hello""");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void IfElse_Draft7_DataMatchesIf_ElseIgnored_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""type"":""integer""},""else"":{""maxLength"":3}}");
        JsonNode? data = JsonNode.Parse("42");
        Assert.IsTrue(data.IsValid(schema));
    }

    // if / then / else

    [TestMethod]
    public void IfThenElse_Draft7_MatchesIf_ThenBranch_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""exclusiveMaximum"":0},""then"":{""minimum"":-10},""else"":{""multipleOf"":2}}");
        JsonNode? data = JsonNode.Parse("-1");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void IfThenElse_Draft7_MatchesIf_ThenBranch_Invalid()
    {
        JSchema schema = Parse(@"{""if"":{""exclusiveMaximum"":0},""then"":{""minimum"":-10},""else"":{""multipleOf"":2}}");
        JsonNode? data = JsonNode.Parse("-100");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void IfThenElse_Draft7_FailsIf_ElseBranch_Valid()
    {
        JSchema schema = Parse(@"{""if"":{""exclusiveMaximum"":0},""then"":{""minimum"":-10},""else"":{""multipleOf"":2}}");
        JsonNode? data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void IfThenElse_Draft7_FailsIf_ElseBranch_Invalid()
    {
        JSchema schema = Parse(@"{""if"":{""exclusiveMaximum"":0},""then"":{""minimum"":-10},""else"":{""multipleOf"":2}}");
        JsonNode? data = JsonNode.Parse("3");
        Assert.IsFalse(data.IsValid(schema));
    }

    // if with boolean schema

    [TestMethod]
    public void If_Draft7_BooleanTrue_AlwaysTakesElseBranch_Invalid()
    {
        JSchema schema = Parse(@"{""if"":true,""then"":{""const"":""then""},""else"":{""const"":""else""}}");
        JsonNode? data = JsonNode.Parse(@"""else""");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void If_Draft7_BooleanFalse_AlwaysTakesElseBranch_Valid()
    {
        JSchema schema = Parse(@"{""if"":false,""then"":{""const"":""then""},""else"":{""const"":""else""}}");
        JsonNode? data = JsonNode.Parse(@"""else""");
        Assert.IsTrue(data.IsValid(schema));
    }

    // lone if / then / else (no paired if)

    [TestMethod]
    public void LoneIf_Draft7_AlwaysValid()
    {
        JSchema schema = Parse(@"{""if"":{""const"":0}}");
        Assert.IsTrue(JsonNode.Parse("0").IsValid(schema));
        Assert.IsTrue(JsonNode.Parse(@"""hello""").IsValid(schema));
    }

    [TestMethod]
    public void LoneThen_Draft7_AlwaysIgnored_Valid()
    {
        JSchema schema = Parse(@"{""then"":{""const"":0}}");
        Assert.IsTrue(JsonNode.Parse("1").IsValid(schema));
    }

    [TestMethod]
    public void LoneElse_Draft7_AlwaysIgnored_Valid()
    {
        JSchema schema = Parse(@"{""else"":{""const"":0}}");
        Assert.IsTrue(JsonNode.Parse("1").IsValid(schema));
    }
}
