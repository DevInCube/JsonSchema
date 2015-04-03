using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using Moq;
using System.IO;
using System.Text;

namespace My.Json.Schema.Tests
{
    [TestClass]
    public class JSchemaTests
    {

        #region jschema_tests
        [TestMethod]
        public void JSchema_ParseEmptySchema_InitialStateOK()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreEqual(null, jschema.Id);
            Assert.AreEqual(null, jschema.Title);
            Assert.AreEqual(null, jschema.Description);
            Assert.AreEqual(null, jschema.Default);
            Assert.AreEqual(null, jschema.Format);
            Assert.AreEqual(JSchemaType.None, jschema.Type);

            Assert.IsTrue(jschema.Items.IsSchema);
            Assert.AreNotEqual(null, jschema.Items.Schema);
            Assert.IsFalse(jschema.Items.IsArray);
            Assert.AreEqual(null, jschema.Items.Array);

            Assert.AreNotEqual(null, jschema.Properties);
            Assert.AreEqual(0, jschema.Properties.Count);
            Assert.AreEqual(null, jschema.MultipleOf);
            Assert.AreEqual(null, jschema.Maximum);
            Assert.AreEqual(null, jschema.Minimum);
            Assert.AreEqual(null, jschema.MaxLength);
            Assert.AreEqual(null, jschema.MinLength);
            Assert.AreEqual(null, jschema.MinLength);
            Assert.AreEqual(null, jschema.MinItems);
            Assert.AreEqual(null, jschema.MaxItems);
            Assert.IsFalse(jschema.UniqueItems);
            Assert.AreEqual(null, jschema.Required);
            Assert.AreEqual(null, jschema.Enum);
            Assert.IsTrue(jschema.AllowAdditionalProperties, "AllowAdditionalProperties");
            Assert.AreNotEqual(null, jschema.PatternProperties, "PatternProperties");          
        }
        [TestMethod]
        public void JSchema_EmptySchemaCompare_AreNotEqual()
        {
            JSchema jschema = JSchema.Parse(@"{}");   
         
            Assert.AreNotEqual(new JSchema(), jschema);            
        }
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException), "schema")]
        public void JSchema_ParseNull_ThrowsArgumentNullException()
        {
            JSchema jschema = JSchema.Parse(null);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void JSchema_ParseEmptyString_ThrowsJSchemaException()
        {
            JSchema jschema = JSchema.Parse("");
        }
        [TestMethod]
        public void JSchema_EmptyToString_IsEmptyJObjectString()
        {
            JSchema jschema = new JSchema();
            Assert.AreEqual("{}", jschema.ToString());
        }
        #endregion

        #region id_tests
        [TestMethod]
        public void Id_SetAbsoluteValidUri_IsValidAndMatches()
        {
            JSchema jschema = JSchema.Parse(@"{id:'http://x.y.z/rootschema.json#'}");
            Assert.AreEqual("http://x.y.z/rootschema.json#", jschema.Id.OriginalString);
        }
        [TestMethod]
        public void Id_SetAsString_IsValidAndMatches()
        {
            JSchema jschema = JSchema.Parse(@"{id:'stringId'}");
            Assert.AreEqual("stringId", jschema.Id.OriginalString);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Id_SetAsObject_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{id:{}}");            
        }
        #endregion

        #region title_tests

        [TestMethod]
        public void JSchema_ParseEmptyTitle_TitleNull()
        {
            JSchema jschema = JSchema.Parse(@"{'title' :,}");
            Assert.AreEqual(null, jschema.Title);
        }
        [TestMethod]
        public void JSchema_ParseNullTitle_TitleNull()
        {
            JSchema jschema = JSchema.Parse(@"{'title' : null}");

            Assert.AreEqual(null, jschema.Title);
        }
        [TestMethod]
        public void JSchema_ParseWithStringTitle_TitleMatch()
        {
            JSchema jschema = JSchema.Parse(@"{'title' : 'test'}");
            Assert.AreEqual("test", jschema.Title);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void JSchema_ParseWithObjectTitle_ThrowsJSchemaValidationError()
        {
            JSchema jschema = JSchema.Parse(@"{'title' : {}}");
        }
        #endregion
        #region description_tests

        [TestMethod]
        public void JSchema_ParseEmptyDescription_DescriptionNull()
        {
            JSchema jschema = JSchema.Parse(@"{'description' :,}");
            Assert.AreEqual(null, jschema.Description);
        }
        [TestMethod]
        public void JSchema_ParseNullDescription_DescriptionNull()
        {
            JSchema jschema = JSchema.Parse(@"{'description' : null}");

            Assert.AreEqual(null, jschema.Description);
        }
        [TestMethod]
        public void JSchema_ParseWithStringDescription_DescriptionMatch()
        {
            JSchema jschema = JSchema.Parse(@"{'description' : 'test'}");
            Assert.AreEqual("test", jschema.Description);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void JSchema_ParseWithObjectDescription_ThrowsJSchemaValidationError()
        {
            JSchema jschema = JSchema.Parse(@"{'description' : {}}");
        }
        #endregion
        #region default_tests

        [TestMethod]
        public void Default_SetString_MatchesJValueString()
        {
            JSchema jschema = JSchema.Parse(@"{'default':'string'}");
            Assert.AreEqual(new JValue("string"), jschema.Default);
        }
        [TestMethod]
        public void Default_SetEmptyJObject_IsInstanceOfJObject()
        {
            JSchema jschema = JSchema.Parse(@"{'default':{}}");
            Assert.IsInstanceOfType(jschema.Default, typeof(JObject));
        }
        #endregion
        #region format_tests

        [TestMethod]
        public void Format_IsSet_MatchString()
        {
            JSchema jschema = JSchema.Parse(@"{'format' : 'test'}");
            Assert.AreEqual("test", jschema.Format);
        }
        #endregion
        
        #region type_tests

        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Type_SetStringNotAType_Throws()
        {
            JSchema jschema = JSchema.Parse(@"{'type':'test'}");
        }
        [TestMethod]
        public void Type_SetStringNullType_IsNullJSchemaType()
        {
            JSchema jschema = JSchema.Parse(@"{'type':'null'}");
            Assert.AreEqual(JSchemaType.Null, jschema.Type);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]        
        public void Type_SetEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'type':[]}");
        }
        [TestMethod]
        public void Type_SetObjectAndNullType_HasTwoTypes()
        {
            JSchema jschema = JSchema.Parse(@"{'type':['object','null']}");

            Assert.AreNotEqual(JSchemaType.None, jschema.Type);
            Assert.IsTrue(jschema.Type.HasFlag(JSchemaType.Null));
            Assert.IsTrue(jschema.Type.HasFlag(JSchemaType.Object));
        }
        #endregion   

        #region referencing_tests
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Ref_SetInvalidReferenceToken_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'$ref':{}}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Ref_SetEmptyReference_ThrowsError()
        {            
            JSchema jschema = JSchema.Parse(@"{'$ref':''}");
        }
        [TestMethod]
        public void Property_SetReferenceSchemaInDefinition_ReferenceResolvedAndHasTypeString()
        {
            JSchema jschema = JSchema.Parse(@"{
    'definitions':{'test':{'type':'string'}},
    'properties' : { 'refTest' : {'$ref' : '#/definitions/test'}},
}");
            var sh = jschema.Properties["refTest"];
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Property_SetExternalReferenceWithoutResolver_ThrowError()
        {
            JSchema jschema = JSchema.Parse(@"{
    'id' : 'http://test.com/schema#',
    'properties' : { 'refTest' : {'$ref' : 'core#/definitions/test'}},
}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Property_SetExternalReferenceWithoutRootId_ThrowError()
        {
            JSchema jschema = JSchema.Parse(@"{
    'properties' : { 'refTest' : {'$ref' : 'core#/definitions/test'}},
}");
        }
        [TestMethod]
        public void Property_SetExternalReferenceWithResolver_ReferenceResolvedAndHasTypeString()
        {
            var mock = new Mock<JSchemaResolver>();
            mock.Setup(ins => ins.GetSchemaResource(new Uri("http://test.com/core#/definitions/test")))
                .Returns(new MemoryStream(
                    Encoding.UTF8.GetBytes("{ definitions : { 'test' : {'type' : 'string'} } }")));

            JSchema jschema = JSchema.Parse(@"{
    'id' : 'http://test.com/schema#',
    'properties' : { 'refTest' : {'$ref' : 'core#/definitions/test'}},
}", mock.Object);
            var sh = jschema.Properties["refTest"];
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
        }

        /// <summary>
        /// JSON.NET Schema doesn't support this
        /// </summary>
        [TestMethod]
        public void Reference_InlineDereferencing_OK()
        {
            string shStr = @"{
    'id': 'http://some.site/schema#',
    'definitions': {
        'schema1': {
            'id': '#inner',
            'type': 'boolean'
        }
    },
    'properties' : { 'refTest' : {'$ref': '#inner'}}
}";
            JSchema jschema = JSchema.Parse(shStr);
            var sh = jschema.Properties["refTest"];
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
        }
        #endregion

        #region items_tests

        #endregion

        #region properties_tests
        [TestMethod]
        public void Properties_SetEmptyObject_IsEmptyArray()
        {
            JSchema jschema = JSchema.Parse(@"{'properties':{}}");

            Assert.AreNotEqual(null, jschema.Properties);
            Assert.AreEqual(0, jschema.Properties.Count);
        }
        [TestMethod]
        public void Properties_SetOneEmptyPropertyObject_PropertyIsInDict()
        {
            JSchema jschema = JSchema.Parse(@"{'properties':{'test':{}}}");

            Assert.AreNotEqual(null, jschema.Properties["test"]);
            Assert.AreEqual(1, jschema.Properties.Count);
        }
        #endregion

        #region patternProperties_tests
        [TestMethod]
        public void patternProperties_SetEmptyObject_IsEmptyArray()
        {
            JSchema jschema = JSchema.Parse(@"{'patternProperties':{}}");

            Assert.AreNotEqual(null, jschema.PatternProperties);
            Assert.AreEqual(0, jschema.PatternProperties.Count);
        }
        [TestMethod]
        public void patternProperties_SetOneEmptyPropertyObject_PropertyIsInDict()
        {
            JSchema jschema = JSchema.Parse(@"{'patternProperties':{'test':{}}}");

            Assert.AreNotEqual(null, jschema.PatternProperties["test"]);
            Assert.AreEqual(1, jschema.PatternProperties.Count);
        }
        #endregion

        #region multipleOf_tests

        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MultipleOf_SetAsString_ThrowError()
        {
            JSchema jschema = JSchema.Parse(@"{'multipleOf':'string'}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MultipleOf_ParseAsZero_ThrowError()
        {
            JSchema jschema = JSchema.Parse(@"{'multipleOf':0}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MultipleOf_SetAsZero_ThrowError()
        {
            JSchema jschema = new JSchema();
            jschema.MultipleOf = 0;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MultipleOf_ParseAsNegativeNumber_ThrowError()
        {
            JSchema jschema = JSchema.Parse(@"{'multipleOf':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MultipleOf_SetAsNegativeNumber_ThrowError()
        {
            JSchema jschema = new JSchema();
            jschema.MultipleOf = -2;
        }
        [TestMethod]
        public void MultipleOf_SetPositiveNumber_MatchesDoubleNumber()
        {
            JSchema jschema = JSchema.Parse(@"{'multipleOf':2}");

            Assert.AreEqual(2D, jschema.MultipleOf);
        }
        #endregion

        #region maximum_tests

        [TestMethod]
        public void Maximum_ParseAsNumber_MatchesDoubleNumber()
        {
            JSchema jschema = JSchema.Parse(@"{'maximum':2}");

            Assert.AreEqual(2D, jschema.Maximum);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Maximum_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maximum':'string'}");
        }
        [TestMethod]
        public void ExclusiveMaximum_NotSet_IsFalse()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.IsFalse(jschema.ExclusiveMaximum);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void ExclusiveMaximum_IsSetButNoMaximum_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'exclusiveMaximum':5}");
        }
        #endregion

        #region minimum_tests
        [TestMethod]
        public void Minimum_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");

        }
        [TestMethod]
        public void Minimum_ParseAsNumber_MatchesDoubleNumber()
        {
            JSchema jschema = JSchema.Parse(@"{'minimum':2}");

            Assert.AreEqual(2D, jschema.Minimum);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Minimum_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minimum':'string'}");
        }
        [TestMethod]
        public void ExclusiveMinimum_NotSet_IsFalse()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.IsFalse(jschema.ExclusiveMinimum);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void ExclusiveMinimum_IsSetButNoMinimum_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'exclusiveMinimum':5}");
        }
        #endregion

        #region maxLength_tests

        [TestMethod]
        public void MaxLength_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'maxLength':2}");

            Assert.AreEqual(2, jschema.MaxLength);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxLength_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxLength':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxLength_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxLength':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxLength_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MaxLength = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxLength_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxLength':'string'}");
        }
        #endregion

        #region minLength_tests

        [TestMethod]
        public void MinLength_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'minLength':2}");

            Assert.AreEqual(2, jschema.MinLength);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinLength_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minLength':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinLength_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minLength':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinLength_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MinLength = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinLength_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minLength':'string'}");
        }
        #endregion

        #region pattern
        [TestMethod]
        public void Pattern_ParseAsString_MatchesString()
        {
            JSchema jschema = JSchema.Parse(@"{'pattern':'test'}");

            Assert.AreEqual("test", jschema.Pattern);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Pattern_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'pattern':2.1}");
        }
        #endregion

        #region minItems_tests

        [TestMethod]
        public void MinItems_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'minItems':2}");

            Assert.AreEqual(2, jschema.MinItems);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinItems_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minItems':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinItems_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minItems':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinItems_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MinItems = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinItems_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minItems':'string'}");
        }
        #endregion

        #region maxItems_tests

        [TestMethod]
        public void MaxItems_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'maxItems':2}");

            Assert.AreEqual(2, jschema.MaxItems);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxItems_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxItems':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxItems_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxItems':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]        
        public void MaxItems_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MaxItems = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxItems_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxItems':'string'}");
        }
        #endregion

        #region minProperties_tests
        [TestMethod]
        public void MinProperties_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreEqual(null, jschema.MinProperties);
        }
        [TestMethod]
        public void MinProperties_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'minProperties':2}");

            Assert.AreEqual(2, jschema.MinProperties);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinProperties_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minProperties':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinProperties_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minProperties':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinProperties_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MinProperties = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MinProperties_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'minProperties':'string'}");
        }
        #endregion

        #region maxProperties_tests
        [TestMethod]
        public void MaxProperties_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreEqual(null, jschema.MaxProperties);
        }
        [TestMethod]
        public void MaxProperties_ParseAsPositiveInteger_MatchesInteger()
        {
            JSchema jschema = JSchema.Parse(@"{'maxProperties':2}");

            Assert.AreEqual(2, jschema.MaxProperties);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxProperties_ParseAsNumber_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxProperties':2.1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxProperties_ParseAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxProperties':-1}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxProperties_SetAsNegativeInteger_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.MaxProperties = -1;
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void MaxProperties_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'maxProperties':'string'}");
        }
        #endregion

        #region uniqueItems
        [TestMethod]
        public void UniqueItems_NotSet_IsFalse()
        {
            JSchema jschema = JSchema.Parse(@"{}");

        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void UniqueItems_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'uniqueItems':'string'}");
        }
        [TestMethod]
        public void UniqueItems_ParseAsBoolean_OK()
        {
            JSchema jschema = JSchema.Parse(@"{uniqueItems:true}");

            Assert.AreEqual(true, jschema.UniqueItems);
        }
        #endregion

        #region required
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void required_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'required':'string'}");
        }
        [TestMethod]
        public void required_ParseOneItemArrayString_OK()
        {
            JSchema jschema = JSchema.Parse(@"{required:['string']}");

            Assert.AreEqual(1, jschema.Required.Count);
            Assert.AreEqual("string", jschema.Required[0]);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void required_ParseNotUniqueStringArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{required:['string','string']}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void required_ParseIntegerArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{required:[0]}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void required_ParseEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{required:[]}");
        }
        #endregion

        #region enum
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void enum_ParseAsString_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'enum':'string'}");
        }
        [TestMethod]
        public void enum_ParseOneItemArrayString_OK()
        {
            JSchema jschema = JSchema.Parse(@"{enum:['string']}");

            Assert.AreEqual(1, jschema.Enum.Count);
            Assert.AreEqual("string", jschema.Enum[0]);
        }
        [TestMethod]
        public void enum_ParseItemsArrayNumber_OK()
        {
            JSchema jschema = JSchema.Parse(@"{enum:[0,2]}");

            Assert.AreEqual(2, jschema.Enum.Count);
            Assert.AreEqual(0, jschema.Enum[0]);
            Assert.AreEqual(2, jschema.Enum[1]);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void enum_ParseNotUniqueStringArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{enum:['string','string']}");
        }
        [TestMethod]
        public void enum_ParseArrayWithDiffTypes_ShouldMatch()
        {
            JSchema jschema = JSchema.Parse(@"{enum:['string',0, {}]}");

            Assert.AreEqual(3, jschema.Enum.Count);
            Assert.AreEqual("string", jschema.Enum[0]);
            Assert.AreEqual(0, jschema.Enum[1]);
            Assert.IsTrue(JToken.DeepEquals(new JObject(), jschema.Enum[2]));
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void enum_ParseEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{enum:[]}");
        }
        #endregion

        #region AllowAdditionalProperties
        [TestMethod]
        public void AllowAdditionalProperties_ParseAsFalseBool_OK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalProperties:false}");

            Assert.IsFalse(jschema.AllowAdditionalProperties);
        }
        [TestMethod]
        public void AllowAdditionalProperties_ParseAsTrueBool_OK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalProperties:true}");

            Assert.IsTrue(jschema.AllowAdditionalProperties);
        }
        #endregion

        #region dependencies
        #endregion
    }
}
