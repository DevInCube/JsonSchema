using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaTests_Draft6
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft6);

    #region Schema version detection

    [TestMethod]
    public void SchemaVersion_Draft6_DetectedFromSchemaKeyword()
    {
        JSchema subject = JSchema.Parse(@"{""$schema"":""http://json-schema.org/draft-06/schema#"",""exclusiveMinimum"":3}");
        Assert.AreEqual(3D, subject.ExclusiveMinimum);
    }

    [TestMethod]
    public void SchemaVersion_Draft4_DetectedFromSchemaKeyword_OverridesDefault()
    {
        // Default is draft-06 but $schema says draft-04 — draft-04 behaviour must apply.
        JSchema subject = JSchema.Parse(
            @"{""$schema"":""http://json-schema.org/draft-04/schema#"",""maximum"":5,""exclusiveMaximum"":true}");
        Assert.AreEqual(5D, subject.ExclusiveMaximum);
    }

    #endregion

    #region $id keyword

    [TestMethod]
    public void Id_Draft6_SetUsingDollarIdKeyword_IsRecognized()
    {
        JSchema subject = Parse(@"{""$id"":""http://example.com/schema#""}");
        Assert.AreEqual(new Uri("http://example.com/schema#"), subject.Id);
    }

    [TestMethod]
    public void Id_Draft6_PlainIdKeyword_TreatedAsExtensionData()
    {
        JSchema subject = Parse(@"{""id"":""http://example.com/schema#""}");
        Assert.IsNull(subject.Id);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("id"));
    }

    #endregion

    #region exclusiveMaximum / exclusiveMinimum (draft-06 numeric)

    [TestMethod]
    public void ExclusiveMaximum_Draft6_AsNumber_IsSet()
    {
        JSchema subject = Parse(@"{""exclusiveMaximum"":5}");
        Assert.AreEqual(5D, subject.ExclusiveMaximum);
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft6_AsNumber_IsSet()
    {
        JSchema subject = Parse(@"{""exclusiveMinimum"":3}");
        Assert.AreEqual(3D, subject.ExclusiveMinimum);
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft6_AsBoolean_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""exclusiveMaximum"":true}"));
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft6_AsBoolean_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""exclusiveMinimum"":false}"));
    }

    #endregion

    #region required

    [TestMethod]
    public void Required_Draft6_EmptyArray_IsValid()
    {
        JSchema subject = Parse(@"{""required"":[]}");
        Assert.IsNotNull(subject);
        Assert.IsEmpty(subject.Required);
    }

    #endregion

    #region Boolean schemas

    [TestMethod]
    public void BooleanSchema_Draft6_True_IsAlwaysValid()
    {
        JSchema subject = JSchema.Parse("true", defaultVersion: SchemaVersion.Draft6);
        Assert.AreEqual(true, subject.IsAlwaysValid);
    }

    [TestMethod]
    public void BooleanSchema_Draft6_False_IsAlwaysInvalid()
    {
        JSchema subject = JSchema.Parse("false", defaultVersion: SchemaVersion.Draft6);
        Assert.AreEqual(false, subject.IsAlwaysValid);
    }

    [TestMethod]
    public void BooleanSchema_Draft6_TrueInSubSchema_IsValid()
    {
        JSchema subject = Parse(@"{""not"":false}");
        Assert.IsNotNull(subject.Not);
        Assert.AreEqual(false, subject.Not!.IsAlwaysValid);
    }

    #endregion

    #region Draft-06-only keywords

    [TestMethod]
    public void Const_Draft6_IsParsed()
    {
        JSchema subject = Parse(@"{""const"":42}");
        Assert.IsNotNull(subject.Const);
    }

    [TestMethod]
    public void Contains_Draft6_IsParsed()
    {
        JSchema subject = Parse(@"{""contains"":{""type"":""integer""}}");
        Assert.IsNotNull(subject.Contains);
    }

    [TestMethod]
    public void PropertyNames_Draft6_IsParsed()
    {
        JSchema subject = Parse(@"{""propertyNames"":{""maxLength"":3}}");
        Assert.IsNotNull(subject.PropertyNames);
    }

    #endregion
}
