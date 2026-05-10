using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaTests_Draft4
{
    private static JSchema Parse(string json) => JSchema.Parse(json, defaultVersion: SchemaVersion.Draft4);

    #region Schema version detection

    [TestMethod]
    public void SchemaVersion_Draft4_DetectedFromSchemaKeyword()
    {
        JSchema subject = JSchema.Parse(@"{""$schema"":""http://json-schema.org/draft-04/schema#"",""minimum"":3,""exclusiveMinimum"":true}");
        Assert.AreEqual(3D, subject.ExclusiveMinimum);
    }

    #endregion

    #region id keyword (draft-04 uses "id" not "$id")

    [TestMethod]
    public void Id_Draft4_SetUsingIdKeyword_IsRecognized()
    {
        JSchema subject = Parse(@"{""id"":""http://example.com/schema#""}");
        Assert.AreEqual(new Uri("http://example.com/schema#"), subject.Id);
    }

    [TestMethod]
    public void Id_Draft4_DollarIdKeyword_TreatedAsExtensionData()
    {
        JSchema subject = Parse(@"{""$id"":""http://example.com/schema#""}");
        Assert.IsNull(subject.Id);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("$id"));
    }

    #endregion

    #region exclusiveMaximum (draft-04 boolean paired with maximum)

    [TestMethod]
    public void ExclusiveMaximum_Draft4_TrueWithMaximum_SetsExclusiveMaximum()
    {
        JSchema subject = Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        Assert.AreEqual(5D, subject.ExclusiveMaximum);
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_FalseWithMaximum_DoesNotSetExclusiveMaximum()
    {
        JSchema subject = Parse(@"{""maximum"":5,""exclusiveMaximum"":false}");
        Assert.IsNull(subject.ExclusiveMaximum);
        Assert.AreEqual(5D, subject.Maximum);
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_TrueBeforeMaximum_SetsExclusiveMaximum()
    {
        JSchema subject = Parse(@"{""exclusiveMaximum"":true,""maximum"":5}");
        Assert.AreEqual(5D, subject.ExclusiveMaximum);
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_TrueWithoutMaximum_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""exclusiveMaximum"":true}"));
    }

    [TestMethod]
    public void ExclusiveMaximum_Draft4_NumberValue_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""maximum"":5,""exclusiveMaximum"":5}"));
    }

    #endregion

    #region exclusiveMinimum (draft-04 boolean paired with minimum)

    [TestMethod]
    public void ExclusiveMinimum_Draft4_TrueWithMinimum_SetsExclusiveMinimum()
    {
        JSchema subject = Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        Assert.AreEqual(3D, subject.ExclusiveMinimum);
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft4_FalseWithMinimum_DoesNotSetExclusiveMinimum()
    {
        JSchema subject = Parse(@"{""minimum"":3,""exclusiveMinimum"":false}");
        Assert.IsNull(subject.ExclusiveMinimum);
        Assert.AreEqual(3D, subject.Minimum);
    }

    [TestMethod]
    public void ExclusiveMinimum_Draft4_TrueWithoutMinimum_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""exclusiveMinimum"":true}"));
    }

    #endregion

    #region required

    [TestMethod]
    public void Required_Draft4_EmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""required"":[]}"));
    }

    #endregion

    #region Boolean schemas

    [TestMethod]
    public void BooleanSchema_Draft4_True_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse("true", defaultVersion: SchemaVersion.Draft4));
    }

    [TestMethod]
    public void BooleanSchema_Draft4_False_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse("false", defaultVersion: SchemaVersion.Draft4));
    }

    [TestMethod]
    public void BooleanSchema_Draft4_InSubSchema_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = Parse(@"{""additionalProperties"":true,""not"":false}"));
    }

    #endregion

    #region Draft-06-only keywords treated as extension data in draft-04

    [TestMethod]
    public void Const_Draft4_TreatedAsExtensionData()
    {
        JSchema subject = Parse(@"{""const"":42}");
        Assert.IsNull(subject.Const);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("const"));
    }

    [TestMethod]
    public void Contains_Draft4_TreatedAsExtensionData()
    {
        JSchema subject = Parse(@"{""contains"":{""type"":""integer""}}");
        Assert.IsNull(subject.Contains);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("contains"));
    }

    [TestMethod]
    public void PropertyNames_Draft4_TreatedAsExtensionData()
    {
        JSchema subject = Parse(@"{""propertyNames"":{""maxLength"":3}}");
        Assert.IsNull(subject.PropertyNames);
        Assert.IsTrue(subject.ExtensionData.ContainsKey("propertyNames"));
    }

    #endregion
}
