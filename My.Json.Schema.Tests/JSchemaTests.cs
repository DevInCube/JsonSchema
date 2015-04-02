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
        public void Id_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Id);
        }
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
        public void JSchema_ParseNoTitle_TitleIsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Title);
        }
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
        public void JSchema_ParseNoDescription_DescriptionIsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Description);
        }
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
        public void Default_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Default);
        }
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
        public void Format_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Format);
        }
        [TestMethod]
        public void Format_IsSet_MatchString()
        {
            JSchema jschema = JSchema.Parse(@"{'format' : 'test'}");
            Assert.AreEqual("test", jschema.Format);
        }
        #endregion
        
        #region type_tests
        [TestMethod]
        public void Type_NotSet_IsNoneType()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(JSchemaType.None, jschema.Type);
        }
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
        [TestMethod]
        public void Items_NotSet_IsEmptyJSchemaNotArray()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.IsTrue(jschema.Items.IsSchema);
            Assert.AreNotEqual(null, jschema.Items.Schema);
            Assert.IsFalse(jschema.Items.IsArray);
            Assert.AreEqual(null, jschema.Items.Array);
        }
        #endregion

        #region properties_tests
        [TestMethod]
        public void Properties_NotSet_IsEmptyArray()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreNotEqual(null, jschema.Properties);
            Assert.AreEqual(0, jschema.Properties.Count);
        }
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

        #region multipleOf_tests
        [TestMethod]
        public void MultipleOf_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreEqual(null, jschema.MultipleOf);
        }
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
        public void Maximum_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.AreEqual(null, jschema.Maximum);
        }
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

            Assert.AreEqual(null, jschema.Minimum);
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
    }
}
