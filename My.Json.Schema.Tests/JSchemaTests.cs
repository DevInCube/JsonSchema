using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Text.Json.Nodes;
using System;
using System.IO;
using System.Text;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaTests
{
    #region JSchema_tests
    [TestMethod]
    public void JSchema_ParseEmptySchema_InitialStateOK()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.AreEqual(null, subject.Id, "id");
        Assert.AreEqual(null, subject.Title, "Title");
        Assert.AreEqual(null, subject.Description, "Description");
        Assert.AreEqual(null, subject.Default, "Default");
        Assert.AreEqual(null, subject.Format, "Format");
        Assert.AreEqual(JSchemaType.None, subject.Type, "Type");

        Assert.AreNotEqual(null, subject.ItemsSchema, "ItemsSchema");
        Assert.AreNotEqual(null, subject.ItemsArray, "ItemsArray");
        Assert.AreEqual(0, subject.ItemsArray.Count, "ItemsArray.Count");

        Assert.AreNotEqual(null, subject.Properties, "Properties");
        Assert.AreEqual(0, subject.Properties.Count, "Properties.Count");
        Assert.AreEqual(null, subject.MultipleOf, "MultipleOf");
        Assert.AreEqual(null, subject.Maximum, "Maximum");
        Assert.AreEqual(null, subject.Minimum, "Minimum");
        Assert.AreEqual(null, subject.MaxLength, "MaxLength");
        Assert.AreEqual(null, subject.MinLength, "MinLength");
        Assert.AreEqual(null, subject.MinItems, "MinItems");
        Assert.AreEqual(null, subject.MaxItems, "MaxItems");
        Assert.IsFalse(subject.UniqueItems, "UniqueItems");
        Assert.AreNotEqual(null, subject.Required, "Required");
        Assert.AreEqual(0, subject.Required.Count, "Required.Count");
        Assert.AreNotEqual(null, subject.Enum, "Enum");
        Assert.AreEqual(0, subject.Enum.Count, "Enum");
        Assert.IsTrue(subject.AllowAdditionalProperties, "AllowAdditionalProperties");
        Assert.AreNotEqual(null, subject.PatternProperties, "PatternProperties");
        Assert.AreEqual(0, subject.PatternProperties.Count, "PatternProperties.Count");
        Assert.AreNotEqual(null, subject.SchemaDependencies, "SchemaDependencies");
        Assert.AreEqual(0, subject.SchemaDependencies.Count, "SchemaDependencies.Count");
        Assert.AreNotEqual(null, subject.PropertyDependencies, "PropertyDependencies");
        Assert.AreEqual(0, subject.PropertyDependencies.Count, "PropertyDependencies.Count");
    }

    [TestMethod]
    public void JSchema_EmptySchemaCompare_AreNotEqual()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.AreNotEqual(new JSchema(), subject);
    }

    [TestMethod]
    public void JSchema_ParseNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _ = JSchema.Parse(null));
    }

    [TestMethod]
    public void JSchema_ParseEmptyString_ThrowsJSchemaException()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(""));
    }

    [TestMethod]
    public void JSchema_EmptyToString_IsEmptyJObjectString()
    {
        JSchema subject = new();
        Assert.AreEqual("{}", subject.ToString());
    }

    #endregion

    #region id_tests
    [TestMethod]
    public void Id_SetAbsoluteValidUri_IsValidAndMatches()
    {
        JSchema subject = JSchema.Parse(@"{""id"":""http://x.y.z/rootschema.json#""}");
        Assert.AreEqual(new Uri("http://x.y.z/rootschema.json#"), subject.Id);
    }

    [TestMethod]
    public void Id_SetAsString_IsValidAndMatches()
    {
        JSchema subject = JSchema.Parse(@"{""id"":""stringId""}");
        Assert.AreEqual(new Uri("stringId", UriKind.Relative), subject.Id);
    }

    [TestMethod]
    public void Id_SetAsObject_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""id"":{}}"));
    }

    [TestMethod]
    public void Id_SetAsEmptyFragment_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() =>
        {
            _ = new JSchema()
            {
                Id = new Uri("#", UriKind.Relative)
            };
        });
    }

    [TestMethod]
    public void Id_AlterResolutionScope_IsValidAndMatches()
    {
        string schema = @"{
    ""id"": ""http://x.y.z/rootschema.json#"",
    ""definitions"" : {
        ""schema1"": {
            ""id"": ""#foo""
        },
        ""schema2"": {
            ""id"": ""otherschema.json"",
            ""definitions"" : {
                ""nested"": {
                    ""id"": ""#bar""
                },
                ""alsonested"": {
                    ""id"": ""t/inner.json#a""
                }
            }
        },
        ""schema3"": {
            ""id"": ""some://where.else/completely#""
        },
    },
    ""properties"" : {
        ""test"" : { ""$ref"" : ""otherschema.json#bar"" }
    }
}";
        JSchema subject = JSchema.Parse(schema);

        Assert.AreEqual(new Uri("#bar", UriKind.Relative), subject.Properties["test"].Id);
    }

    #endregion

    #region title_tests

    [TestMethod]
    public void JSchema_ParseNullTitle_TitleNull()
    {
        JSchema subject = JSchema.Parse(@"{""title"" : null}");

        Assert.AreEqual(null, subject.Title);
    }

    [TestMethod]
    public void JSchema_ParseWithStringTitle_TitleMatch()
    {
        JSchema subject = JSchema.Parse(@"{""title"" : ""test""}");
        Assert.AreEqual("test", subject.Title);
    }

    [TestMethod]
    public void JSchema_ParseWithObjectTitle_ThrowsJSchemaValidationError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""title"" : {}}"));
    }
    #endregion
    #region description_tests

    [TestMethod]
    public void JSchema_ParseNullDescription_DescriptionNull()
    {
        JSchema subject = JSchema.Parse(@"{""description"" : null}");

        Assert.AreEqual(null, subject.Description);
    }

    [TestMethod]
    public void JSchema_ParseWithStringDescription_DescriptionMatch()
    {
        JSchema subject = JSchema.Parse(@"{""description"" : ""test""}");
        Assert.AreEqual("test", subject.Description);
    }

    [TestMethod]
    public void JSchema_ParseWithObjectDescription_ThrowsJSchemaValidationError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""description"" : {}}"));
    }
    #endregion
    #region default_tests

    [TestMethod]
    public void Default_SetString_MatchesJValueString()
    {
        JSchema subject = JSchema.Parse(@"{""default"":""string""}");

        Assert.AreEqual("string", subject.Default.GetValue<string>());
    }

    [TestMethod]
    public void Default_SetEmptyJObject_IsInstanceOfJObject()
    {
        JSchema subject = JSchema.Parse(@"{""default"":{}}");

        Assert.IsInstanceOfType<JsonObject>(subject.Default);
    }

    #endregion
    #region format_tests

    [TestMethod]
    public void Format_IsSet_MatchString()
    {
        JSchema subject = JSchema.Parse(@"{""format"" : ""test""}");
        Assert.AreEqual("test", subject.Format);
    }
    #endregion

    #region type_tests

    [TestMethod]
    public void Type_SetStringNotAType_Throws()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""type"":""test""}"));
    }

    [TestMethod]
    public void Type_SetStringNullType_IsNullJSchemaType()
    {
        JSchema subject = JSchema.Parse(@"{""type"":""null""}");
        Assert.AreEqual(JSchemaType.Null, subject.Type);
    }

    [TestMethod]
    public void Type_SetEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""type"":[]}"));
    }

    [TestMethod]
    public void Type_SetNotUniqueArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""type"":[""object"",""object""]}"));
    }

    [TestMethod]
    public void Type_SetObjectAndNullType_HasTwoTypes()
    {
        JSchema subject = JSchema.Parse(@"{""type"":[""object"",""null""]}");

        Assert.AreNotEqual(JSchemaType.None, subject.Type);
        Assert.IsTrue(subject.Type.HasFlag(JSchemaType.Null));
        Assert.IsTrue(subject.Type.HasFlag(JSchemaType.Object));
    }
    #endregion

    #region referencing_tests
    [TestMethod]
    public void Ref_SetInvalidReferenceToken_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""$ref"":{}}"));
    }

    [TestMethod]
    public void Ref_SetEmptyReference_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""$ref"":""""}"));
    }

    [TestMethod]
    public void Property_SetReferenceSchemaInDefinition_ReferenceResolvedAndHasTypeString()
    {
        JSchema subject = JSchema.Parse(@"{
    ""definitions"":{""test"":{""type"":""string""}},
    ""properties"" : { ""refTest"" : {""$ref"" : ""#/definitions/test""}},
}");
        var sh = subject.Properties["refTest"];
        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
    }

    [TestMethod]
    public void Reference_ResolveWithinDefinitions_ReferenceResolvedAndHasTypeString()
    {
        JSchema subject = JSchema.Parse(@"{
    ""definitions"":{
        ""test"":{""type"":""string""},
        ""test2"":{ ""$ref"":""test"" }
    },
    ""properties"" : { ""refTest"" : {""$ref"" : ""#/definitions/test2""}}
}");
        var sh = subject.Properties["refTest"];
        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
    }

    [TestMethod]
    public void Property_SetExternalReferenceWithoutResolver_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{
    ""id"" : ""http://test.com/schema#"",
    ""properties"" : { ""refTest"" : {""$ref"" : ""core#/definitions/test""}},
}"));
    }

    [TestMethod]
    public void Property_SetExternalReferenceWithoutRootId_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{
    ""properties"" : { ""refTest"" : {""$ref"" : ""core#/definitions/test""}},
}"));
    }

    [TestMethod]
    public void Property_SetExternalReferenceWithResolver_ReferenceResolvedAndHasTypeString()
    {
        var mock = new Mock<JSchemaResolver>();
        mock.Setup(ins => ins.GetSchemaResource(new Uri("http://test.com/core#/definitions/test")))
            .Returns(new MemoryStream(
                Encoding.UTF8.GetBytes(@"{ ""definitions"" : { ""test"" : {""type"" : ""string""} } }")));

        JSchema subject = JSchema.Parse(@"{
    ""id"" : ""http://test.com/schema#"",
    ""properties"" : { ""refTest"" : {""$ref"" : ""core#/definitions/test""}},
}", mock.Object);
        var sh = subject.Properties["refTest"];
        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
    }

    [TestMethod]
    public void Ref_SetExternalReferenceWithScopeChanged_ReferenceResolvedAndHasTypeString()
    {
        var mock = new Mock<JSchemaResolver>();
        mock.Setup(ins => ins.GetSchemaResource(new Uri("http://localhost:1234/folder/test.json")))
            .Returns(new MemoryStream(
                Encoding.UTF8.GetBytes(@"{ ""type"" : ""string"" }")));

        JSchema subject = JSchema.Parse(@" {
    ""id"": ""http://localhost:1234/"",
    ""items"": {
        ""id"": ""folder/"",
        ""items"": {""$ref"": ""test.json""}
    }
}", mock.Object);
        var sh = subject.ItemsSchema.ItemsSchema;
        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
    }

    [TestMethod]
    public void Reference_InlineDereferencing_OK()
    {
        string shStr = @"{
    ""id"": ""http://some.site/schema#"",
    ""definitions"": {
        ""schema1"": {
            ""id"": ""#inner"",
            ""type"": ""boolean""
        }
    },
    ""properties"" : { ""refTest"" : {""$ref"": ""#inner""}}
}";
        JSchema subject = JSchema.Parse(shStr);
        var sh = subject.Properties["refTest"];

        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
    }

    [TestMethod]
    public void Reference_InlineDereferencingReverseOrder_OK()
    {
        string shStr = @"{
    ""id"": ""http://some.site/schema#"",
    ""not"": { ""$ref"": ""#inner"" },
    ""definitions"": {
        ""schema1"": {
            ""id"": ""#inner"",
            ""type"": ""boolean""
        }
    }
}";
        JSchema subject = JSchema.Parse(shStr);
        var sh = subject.Not;

        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
    }

    [TestMethod]
    public void Reference_InlineDereferencingWithoutBaseUri_OK()
    {
        string shStr = @"{
    ""not"": { ""$ref"": ""#inner"" },
    ""definitions"": {
        ""schema1"": {
            ""id"": ""#inner"",
            ""type"": ""boolean""
        }
    }
}";
        JSchema subject = JSchema.Parse(shStr);
        var sh = subject.Not;

        Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
    }

    [TestMethod]
    public void Reference_InvalidSchemaInDefinitions_ThrowError()
    {
        string shStr = @"{
    ""definitions"": {
        ""schema1"": {
            ""id"": 1,
            ""type"": ""boolean""
        }
    }
}";
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(shStr));
    }

    [TestMethod]
    public void Reference_SubschemaDiscovery_OK()
    {
        string shStr = @"{
    ""not"": { ""$ref"": ""#/inner"" },
    ""additionalProperties"": { ""$ref"": ""#/inner/schema1"" },
    ""inner"": {
        ""title"" : ""ok"",
        ""schema1"": {
            ""title"" : ""ok/ok"",
            ""id"": ""#inner"",
            ""type"": ""boolean""
        }
    }
}";
        JSchema subject = JSchema.Parse(shStr);

        Assert.AreEqual("ok", subject.Not.Title);
        Assert.AreEqual("ok/ok", subject.AdditionalProperties.Title);
    }

    [TestMethod]
    public void Reference_NonExistingSubschemaDiscovery_ThrowError()
    {
        string shStr = @"{
    ""not"": { ""$ref"": ""#/inner"" },
}";
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(shStr));
    }

    [TestMethod]
    public void Reference_Loop_PointersEquals()
    {
        string shStr = @"{ ""properties"": { ""loop"": { ""$ref"" : ""#"" } }}";
        JSchema subject = JSchema.Parse(shStr);

        var loop = subject.Properties["loop"];
        Assert.AreEqual(subject, loop);
    }

    #endregion

    #region items_tests
    [TestMethod]
    public void Items_ParseNull_ThrowsJSchemaException()
    {
        string shStr = @"{ ""items"": null }";
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(shStr));
    }

    [TestMethod]
    public void Items_ParseAsSchema_SchemaMatches()
    {
        string shStr = @"{ ""items"": { ""type"":""integer"" }}";
        JSchema subject = JSchema.Parse(shStr);

        Assert.IsTrue(subject.ItemsSchema.Type.HasFlag(JSchemaType.Integer));
    }

    [TestMethod]
    public void Items_ParseAsList_SchemasMatch()
    {
        string shStr = @"{ ""items"": [{ ""type"":""integer"" },{ ""type"":""boolean"" } ]}";
        JSchema subject = JSchema.Parse(shStr);

        Assert.AreEqual(2, subject.ItemsArray.Count);
        Assert.IsTrue(subject.ItemsArray[0].Type.HasFlag(JSchemaType.Integer));
        Assert.IsTrue(subject.ItemsArray[1].Type.HasFlag(JSchemaType.Boolean));
    }
    #endregion

    #region properties_tests
    [TestMethod]
    public void Properties_SetEmptyObject_IsEmptyArray()
    {
        JSchema subject = JSchema.Parse(@"{""properties"":{}}");

        Assert.AreNotEqual(null, subject.Properties);
        Assert.AreEqual(0, subject.Properties.Count);
    }

    [TestMethod]
    public void Properties_SetOneEmptyPropertyObject_PropertyIsInDict()
    {
        JSchema subject = JSchema.Parse(@"{""properties"":{""test"":{}}}");

        Assert.AreNotEqual(null, subject.Properties["test"]);
        Assert.AreEqual(1, subject.Properties.Count);
    }
    #endregion

    #region patternProperties_tests
    [TestMethod]
    public void patternProperties_SetEmptyObject_IsEmptyArray()
    {
        JSchema subject = JSchema.Parse(@"{""patternProperties"":{}}");

        Assert.AreNotEqual(null, subject.PatternProperties);
        Assert.AreEqual(0, subject.PatternProperties.Count);
    }
    #endregion

    #region multipleOf_tests

    [TestMethod]
    public void MultipleOf_SetAsString_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""multipleOf"":""string""}"));
    }

    [TestMethod]
    public void MultipleOf_ParseAsZero_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""multipleOf"":0}"));
    }

    [TestMethod]
    public void MultipleOf_SetAsZero_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MultipleOf = 0
        });
    }

    [TestMethod]
    public void MultipleOf_ParseAsNegativeNumber_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""multipleOf"":-1}"));
    }

    [TestMethod]
    public void MultipleOf_SetAsNegativeNumber_ThrowError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema() { MultipleOf = -2 });
    }

    [TestMethod]
    public void MultipleOf_SetPositiveNumber_MatchesDoubleNumber()
    {
        JSchema subject = JSchema.Parse(@"{""multipleOf"":2}");

        Assert.AreEqual(2D, subject.MultipleOf);
    }

    [TestMethod]
    public void MultipleOf_SetDoubleNumber_MatchesDoubleNumber()
    {
        JSchema subject = JSchema.Parse(@"{""multipleOf"":0.5}");

        Assert.AreEqual(0.5D, subject.MultipleOf);
    }
    #endregion

    #region maximum_tests

    [TestMethod]
    public void Maximum_ParseAsNumber_MatchesDoubleNumber()
    {
        JSchema subject = JSchema.Parse(@"{""maximum"":2}");

        Assert.AreEqual(2D, subject.Maximum);
    }

    [TestMethod]
    public void Maximum_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maximum"":""string""}"));
    }

    [TestMethod]
    public void ExclusiveMaximum_NotSet_IsFalse()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.IsFalse(subject.ExclusiveMaximum);
    }

    [TestMethod]
    public void ExclusiveMaximum_ParseIsSetButNoMaximum_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""exclusiveMaximum"":true}"));
    }

    [TestMethod]
    public void ExclusiveMaximum_ParseIsSetToFalseButNoMaximum_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""exclusiveMaximum"":false}"));
    }

    [TestMethod]
    public void ExclusiveMaximum_ParseIsSetTrue_IsTrue()
    {
        JSchema subject = JSchema.Parse(@"{""maximum"":1, ""exclusiveMaximum"":true}");

        Assert.IsTrue(subject.ExclusiveMaximum);
    }

    [TestMethod]
    public void ExclusiveMaximum_ParseIsSetBeforeMaximum_IsTrue()
    {
        JSchema subject = JSchema.Parse(@"{""exclusiveMaximum"":true, ""maximum"":1}");

        Assert.IsTrue(subject.ExclusiveMaximum);
    }
    #endregion

    #region minimum_tests
    [TestMethod]
    public void Minimum_NotSet_IsNull()
    {
        var subject = JSchema.Parse(@"{}");

        Assert.IsNull(subject.Minimum);
    }

    [TestMethod]
    public void Minimum_ParseAsNumber_MatchesDoubleNumber()
    {
        JSchema subject = JSchema.Parse(@"{""minimum"":2}");

        Assert.AreEqual(2D, subject.Minimum);
    }

    [TestMethod]
    public void Minimum_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minimum"":""string""}"));
    }

    [TestMethod]
    public void ExclusiveMinimum_NotSet_IsFalse()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.IsFalse(subject.ExclusiveMinimum);
    }

    [TestMethod]
    public void ExclusiveMinimum_IsSetButNoMinimum_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""exclusiveMinimum"":5}"));
    }
    #endregion

    #region maxLength_tests

    [TestMethod]
    public void MaxLength_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""maxLength"":2}");

        Assert.AreEqual(2, subject.MaxLength);
    }

    [TestMethod]
    public void MaxLength_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxLength"":2.1}"));
    }

    [TestMethod]
    public void MaxLength_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxLength"":-1}"));
    }

    [TestMethod]
    public void MaxLength_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MaxLength = -1
        });
    }

    [TestMethod]
    public void MaxLength_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxLength"":""string""}"));
    }
    #endregion

    #region minLength_tests

    [TestMethod]
    public void MinLength_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""minLength"":2}");

        Assert.AreEqual(2, subject.MinLength);
    }

    [TestMethod]
    public void MinLength_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minLength"":2.1}"));
    }

    [TestMethod]
    public void MinLength_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minLength"":-1}"));
    }

    [TestMethod]
    public void MinLength_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MinLength = -1
        });
    }

    [TestMethod]
    public void MinLength_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minLength"":""string""}"));
    }
    #endregion

    #region pattern
    [TestMethod]
    public void Pattern_ParseAsString_MatchesString()
    {
        JSchema subject = JSchema.Parse(@"{""pattern"":""test""}");

        Assert.AreEqual("test", subject.Pattern);
    }

    [TestMethod]
    public void Pattern_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""pattern"":2.1}"));
    }

    [TestMethod]
    public void Pattern_SetInvalidRegex_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            Pattern = "*"
        });
    }

    [TestMethod]
    public void Pattern_SetEmptyRegex_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            Pattern = ""
        });
    }
    #endregion

    #region minItems_tests

    [TestMethod]
    public void MinItems_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""minItems"":2}");

        Assert.AreEqual(2, subject.MinItems);
    }

    [TestMethod]
    public void MinItems_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minItems"":2.1}"));
    }

    [TestMethod]
    public void MinItems_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minItems"":-1}"));
    }

    [TestMethod]
    public void MinItems_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MinItems = -1
        });
    }

    [TestMethod]
    public void MinItems_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minItems"":""string""}"));
    }
    #endregion

    #region maxItems_tests

    [TestMethod]
    public void MaxItems_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""maxItems"":2}");

        Assert.AreEqual(2, subject.MaxItems);
    }

    [TestMethod]
    public void MaxItems_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxItems"":2.1}"));
    }

    [TestMethod]
    public void MaxItems_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxItems"":-1}"));
    }

    [TestMethod]
    public void MaxItems_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MaxItems = -1
        });
    }

    [TestMethod]
    public void MaxItems_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxItems"":""string""}"));
    }
    #endregion

    #region minProperties_tests
    [TestMethod]
    public void MinProperties_NotSet_IsNull()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.AreEqual(null, subject.MinProperties);
    }

    [TestMethod]
    public void MinProperties_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""minProperties"":2}");

        Assert.AreEqual(2, subject.MinProperties);
    }

    [TestMethod]
    public void MinProperties_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minProperties"":2.1}"));
    }

    [TestMethod]
    public void MinProperties_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minProperties"":-1}"));
    }

    [TestMethod]
    public void MinProperties_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MinProperties = -1
        });
    }

    [TestMethod]
    public void MinProperties_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""minProperties"":""string""}"));
    }
    #endregion

    #region maxProperties_tests
    [TestMethod]
    public void MaxProperties_NotSet_IsNull()
    {
        JSchema subject = JSchema.Parse(@"{}");

        Assert.AreEqual(null, subject.MaxProperties);
    }

    [TestMethod]
    public void MaxProperties_ParseAsPositiveInteger_MatchesInteger()
    {
        JSchema subject = JSchema.Parse(@"{""maxProperties"":2}");

        Assert.AreEqual(2, subject.MaxProperties);
    }

    [TestMethod]
    public void MaxProperties_ParseAsNumber_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxProperties"":2.1}"));
    }

    [TestMethod]
    public void MaxProperties_ParseAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxProperties"":-1}"));
    }

    [TestMethod]
    public void MaxProperties_SetAsNegativeInteger_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = new JSchema()
        {
            MaxProperties = -1
        });
    }

    [TestMethod]
    public void MaxProperties_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""maxProperties"":""string""}"));
    }
    #endregion

    #region uniqueItems
    [TestMethod]
    public void UniqueItems_NotSet_IsFalse()
    {
        var subject = JSchema.Parse(@"{}");

        Assert.IsFalse(subject.UniqueItems);
    }

    [TestMethod]
    public void UniqueItems_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""uniqueItems"":""string""}"));
    }

    [TestMethod]
    public void UniqueItems_ParseAsBoolean_OK()
    {
        JSchema subject = JSchema.Parse(@"{""uniqueItems"":true}");

        Assert.IsTrue(subject.UniqueItems);
    }
    #endregion

    #region required
    [TestMethod]
    public void Required_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""required"":""string""}"));
    }

    [TestMethod]
    public void Required_ParseOneItemArrayString_OK()
    {
        JSchema subject = JSchema.Parse(@"{""required"":[""string""]}");

        Assert.AreEqual(1, subject.Required.Count);
        Assert.AreEqual("string", subject.Required[0]);
    }

    [TestMethod]
    public void Required_ParseNotUniqueStringArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""required"":[""string"",""string""]}"));
    }

    [TestMethod]
    public void Required_ParseIntegerArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""required"":[0]}"));
    }

    [TestMethod]
    public void Required_ParseEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""required"":[]}"));
    }
    #endregion

    #region enum
    [TestMethod]
    public void Enum_ParseAsString_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""enum"":""string""}"));
    }

    [TestMethod]
    public void Enum_ParseOneItemArrayString_OK()
    {
        JSchema subject = JSchema.Parse(@"{""enum"":[""string""]}");

        Assert.AreEqual(1, subject.Enum.Count);
        Assert.AreEqual("string", subject.Enum[0].GetValue<string>());
    }

    [TestMethod]
    public void Enum_ParseItemsArrayNumber_OK()
    {
        JSchema subject = JSchema.Parse(@"{""enum"":[0,2]}");

        Assert.AreEqual(2, subject.Enum.Count);
        Assert.AreEqual(0, subject.Enum[0].GetValue<int>());
        Assert.AreEqual(2, subject.Enum[1].GetValue<int>());
    }

    [TestMethod]
    public void Enum_ParseNotUniqueStringArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""enum"":[""string"",""string""]}"));
    }

    [TestMethod]
    public void Enum_ParseArrayWithDiffTypes_ShouldMatch()
    {
        JSchema subject = JSchema.Parse(@"{""enum"":[""string"",0, {}]}");

        Assert.AreEqual(3, subject.Enum.Count);
        Assert.AreEqual("string", subject.Enum[0].GetValue<string>());
        Assert.AreEqual(0, subject.Enum[1].GetValue<int>());
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), subject.Enum[2]));
    }

    [TestMethod]
    public void Enum_ParseEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""enum"":[]}"));
    }
    #endregion

    #region AllowAdditionalProperties
    [TestMethod]
    public void AllowAdditionalProperties_ParseAsFalseBool_OK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalProperties"":false}");

        Assert.IsFalse(subject.AllowAdditionalProperties);
    }

    [TestMethod]
    public void AllowAdditionalProperties_ParseAsTrueBool_OK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalProperties"":true}");

        Assert.IsTrue(subject.AllowAdditionalProperties);
    }
    #endregion

    #region additionalProperties
    [TestMethod]
    public void AdditionalProperties_ParseAsEmptyObject_OK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalProperties"":{}}");

        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), JsonObject.Parse(subject.AdditionalProperties.ToString())));
    }
    #endregion

    #region AllowAdditionalItems
    [TestMethod]
    public void AllowAdditionalItems_ParseAsFalseBool_OK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalItems"":false}");

        Assert.IsFalse(subject.AllowAdditionalItems);
    }

    [TestMethod]
    public void AllowAdditionalItems_ParseAsTrueBool_OK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalItems"":true}");

        Assert.IsTrue(subject.AllowAdditionalItems);
    }
    #endregion

    #region AdditionalItems
    [TestMethod]
    public void AdditionalItems_SetEmptySchema_TypeOK()
    {
        JSchema subject = JSchema.Parse(@"{""additionalItems"":{""type"":""boolean""}}");

        Assert.IsTrue(subject.AdditionalItems.Type.HasFlag(JSchemaType.Boolean));
    }
    #endregion

    #region dependencies
    #endregion

    #region allOf
    [TestMethod]
    public void AllOf_ParseAsObject_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""allOf"":{}}"));
    }

    [TestMethod]
    public void AllOf_ParseAsEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""allOf"":[]}"));
    }

    [TestMethod]
    public void AllOf_ParseAsOneItemStringArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""allOf"":[""string""]}"));
    }

    [TestMethod]
    public void AllOf_ParseAsOneItemObjectArray_MatchesSchema()
    {
        JSchema subject = JSchema.Parse(@"{""allOf"":[{}]}");

        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), JsonObject.Parse(subject.AllOf[0].ToString())));
    }
    #endregion

    #region anyOf
    [TestMethod]
    public void AnyOf_ParseAsObject_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""anyOf"":{}}"));
    }

    [TestMethod]
    public void AnyOf_ParseAsEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""anyOf"":[]}"));
    }

    [TestMethod]
    public void AnyOf_ParseAsOneItemStringArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""anyOf"":[""string""]}"));
    }

    [TestMethod]
    public void AnyOf_ParseAsOneItemObjectArray_MatchesSchema()
    {
        JSchema subject = JSchema.Parse(@"{""anyOf"":[{}]}");

        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), JsonObject.Parse(subject.AnyOf[0].ToString())));
    }
    #endregion

    #region oneOf
    [TestMethod]
    public void OneOf_ParseAsObject_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""oneOf"":{}}"));
    }

    [TestMethod]
    public void OneOf_ParseAsEmptyArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""oneOf"":[]}"));
    }

    [TestMethod]
    public void OneOf_ParseAsOneItemStringArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""oneOf"":[""string""]}"));
    }

    [TestMethod]
    public void OneOf_ParseAsOneItemObjectArray_MatchesSchema()
    {
        JSchema subject = JSchema.Parse(@"{""oneOf"":[{}]}");

        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), JsonObject.Parse(subject.OneOf[0].ToString())));
    }
    #endregion

    #region not
    [TestMethod]
    public void Not_ParseAsArray_ThrowsError()
    {
        Assert.Throws<JSchemaException>(() => _ = JSchema.Parse(@"{""not"":[]}"));
    }

    [TestMethod]
    public void Not_ParseAsEmptyObject_Match()
    {
        JSchema subject = JSchema.Parse(@"{""not"":{}}");

        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), JsonObject.Parse(subject.Not.ToString())));
    }
    #endregion

    #region ExtensionData
    [TestMethod]
    public void ExtensionData_ParseDefinitions_IsAddedToExtensionData()
    {
        JSchema subject = JSchema.Parse(@"{""definitions"":{}}");

        Assert.IsTrue(subject.ExtensionData.ContainsKey("definitions"));
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), subject.ExtensionData["definitions"]));
    }

    [TestMethod]
    public void ExtensionData_ParseNotAKeyword_IsAddedToExtensionData()
    {
        JSchema subject = JSchema.Parse(@"{""ext"":{}}");

        Assert.IsTrue(subject.ExtensionData.ContainsKey("ext"));
        Assert.IsTrue(JsonNode.DeepEquals(new JsonObject(), subject.ExtensionData["ext"]));
    }
    #endregion

    [TestMethod]
    public void Ref_ResolutionComplexScope_OK()
    {
        var json = @"{
    ""$schema"":""http://json-schema.org/draft-04/schema#"",
    ""id"":""http://example.com/product/compositor#"",

    ""type"":""object"",
	""format"":""tab"",

	""definitions"" : {

	},

    ""properties"":{

        ""Source"":{

            ""id"":""Source"",
            ""type"":""object"",

			""description"" : ""Lorem Ipsum is simply dummy text of the printing and typesetting industry."",


            ""properties"":{

                ""MasterName"":{
                    ""id"":""MasterName"",
                    ""type"": [""string"", ""null""],
					""format"":""label"",
					""description"" : ""Lorem Ipsum is simply dummy text of the printing and"",
                },

				""SetMaster"":{
                    ""type"":""string"",
                    ""format"":""button"",
					""ignore"":""true"",
					""default"":""Set as Master"",
					""description"" : ""Lorem Ipsum is simply dummy text of the printing and"",
                },

                ""QueueSet"":{
                    ""id"":""QueueSet"",
                    ""type"":""array"",

					""description"" : ""Lorem Ipsum is simply dummy text of the printing and"",

					""format"" : ""list"",

					""Style"" : {
						""MaxHeight"" : 450,
						""MinHeight"" : 450,
					},

                    ""items"": {
                        ""type"":""object"",


						""Style"" : {
							""DisplayMemberPath"" : ""Name"",
						},

                        ""properties"":{
                            ""Name"":{
                                ""id"":""Name"",
                                ""type"":""string"",
								""default"":""name"",
                            },
                            ""Prefix"":{
                                ""id"":""Prefix"",
                                ""type"":""string""
                            },
                            ""Suffix"":{
                                ""id"":""Suffix"",
                                ""type"":""string""
                            },
                            ""Dir"":{
                                ""id"":""Dir"",
                                ""type"":""string""
                            },
                            ""Remove"":{
                                ""id"":""Remove"",
                                ""type"":""boolean"",
								""description"" : ""Lorem Ipsum is simply dummy text of the printing and"",
                            },
                            ""Sink"":{
                                ""id"":""Sink"",
                                ""type"":""boolean""
                            },
                            ""Tout"":{
                                ""id"":""Tout"",
                                ""type"":""integer""
                            },
                        },

						""required"":[ ""Name"", ""Prefix"", ""Suffix"", ""Dir"", ""Remove"", ""Sink"", ""Tout"" ],
						""additionalProperties"": false,
                    }

                }
            },
            ""required"":[ ""MasterName"", ""QueueSet"" ],
			""additionalProperties"": false,
        },

        ""Sink"":{
            ""id"":""Sink"",
            ""type"":""object"",

			""description"" : ""Lorem Ipsum is simply dummy text of the printing and typesetting industry."",

            ""properties"":{
                ""QueueSet"":{
                    ""id"":""QueueSet"",
                    ""type"":""array"",

					""format"" : ""list"",

					""Style"" : {
						""MaxHeight"" : 300,
						""MinHeight"" : 300,
					},

					""items"": { ""$ref"" : ""definitions#/definitions/EventDir"" },
                }
            },

			""required"":[ ""QueueSet"" ],
			""additionalProperties"": false,
        }
    },

    ""required"":[
        ""Source"",
        ""Sink""
    ],
	""additionalProperties"": false
}";
        JSchemaPreloadedResolver res0 = new();
        res0.Add(new Uri("http://example.com/product/definitions"), File.ReadAllText("Resources/common/definitions.txt"));
        var subject = JSchema.Parse(json, res0);

        Assert.IsNotNull(subject);
    }

    #region bignum_tests

    [TestMethod]
    public void Bignum_PositiveIntegerType_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""integer""}");
        JsonNode data = JsonNode.Parse("12345678910111213141516171819202122232425262728293031");

        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Bignum_NegativeIntegerType_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""integer""}");
        JsonNode data = JsonNode.Parse("-12345678910111213141516171819202122232425262728293031");

        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Bignum_PositiveNumberType_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""number""}");
        JsonNode data = JsonNode.Parse("98249283749234923498293171823948729348710298301928331");

        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Bignum_NegativeNumberType_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""number""}");
        JsonNode data = JsonNode.Parse("-98249283749234923498293171823948729348710298301928331");

        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Bignum_StringType_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""string""}");
        JsonNode data = JsonNode.Parse("98249283749234923498293171823948729348710298301928331");

        Assert.IsFalse(data.IsValid(schema));
    }

    #endregion
}
