using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaTests_Draft7
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft7);

    #region Schema version detection

    [TestMethod]
    public void SchemaVersion_Draft7_DetectedFromSchemaKeyword()
    {
        JSchema subject = JSchema.Parse(@"{""$schema"":""http://json-schema.org/draft-07/schema#"",""if"":{""type"":""integer""}}");
        Assert.AreEqual(SchemaVersion.Draft7, subject.Version);
        Assert.IsNotNull(subject.If);
    }

    [TestMethod]
    public void SchemaVersion_Draft6_DetectedFromSchemaKeyword_OverridesDefault()
    {
        JSchema subject = JSchema.Parse(
            @"{""$schema"":""http://json-schema.org/draft-06/schema#"",""exclusiveMinimum"":3}");
        Assert.AreEqual(SchemaVersion.Draft6, subject.Version);
        Assert.AreEqual(3D, subject.ExclusiveMinimum);
    }

    #endregion

    #region if / then / else

    [TestMethod]
    public void If_Draft7_IsParsed()
    {
        JSchema subject = Parse(@"{""if"":{""type"":""integer""}}");
        Assert.IsNotNull(subject.If);
    }

    [TestMethod]
    public void Then_Draft7_IsParsed()
    {
        JSchema subject = Parse(@"{""if"":{""type"":""integer""},""then"":{""minimum"":0}}");
        Assert.IsNotNull(subject.Then);
    }

    [TestMethod]
    public void Else_Draft7_IsParsed()
    {
        JSchema subject = Parse(@"{""if"":{""type"":""integer""},""else"":{""maxLength"":5}}");
        Assert.IsNotNull(subject.Else);
    }

    [TestMethod]
    public void If_Draft4_TreatedAsExtensionData()
    {
        JSchema subject = JSchema.Parse(@"{""if"":{""type"":""integer""}}", defaultVersion: SchemaVersion.Draft4);
        Assert.IsNull(subject.If);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("if"));
    }

    [TestMethod]
    public void If_Draft6_TreatedAsExtensionData()
    {
        JSchema subject = JSchema.Parse(@"{""if"":{""type"":""integer""}}", defaultVersion: SchemaVersion.Draft6);
        Assert.IsNull(subject.If);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("if"));
    }

    #endregion

    #region Boolean schemas

    [TestMethod]
    public void BooleanSchema_Draft7_True_IsAlwaysValid()
    {
        JSchema subject = JSchema.Parse("true", defaultVersion: SchemaVersion.Draft7);
        Assert.AreEqual(true, subject.IsAlwaysValid);
    }

    [TestMethod]
    public void BooleanSchema_Draft7_False_IsAlwaysInvalid()
    {
        JSchema subject = JSchema.Parse("false", defaultVersion: SchemaVersion.Draft7);
        Assert.AreEqual(false, subject.IsAlwaysValid);
    }

    [TestMethod]
    public void BooleanSchema_Draft7_IfBranchTrue_IsParsed()
    {
        JSchema subject = Parse(@"{""if"":true,""then"":{""const"":""yes""}}");
        Assert.AreEqual(true, subject.If!.IsAlwaysValid);
    }

    #endregion
}
