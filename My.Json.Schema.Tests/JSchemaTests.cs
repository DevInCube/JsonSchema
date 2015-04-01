using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace My.Json.Schema.Tests
{
    [TestClass]
    public class JSchemaTests
    {
        #region jschema_tests
        [TestMethod]
        public void JSchema_EmptySchemaCompare_EreEqual()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(new JSchema(), jschema);
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
        public void Type_NotSet_IsNull()
        {
            JSchema jschema = JSchema.Parse(@"{}");
            Assert.AreEqual(null, jschema.Type);
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
        #endregion
        #region items_tests
        [TestMethod]
        public void Items_NotSet_IsEmptyJSchemaNotArray()
        {
            JSchema jschema = JSchema.Parse(@"{}");

            Assert.IsTrue(jschema.Items.IsSchema);
            Assert.AreEqual(new JSchema(), jschema.Items.Schema);
            Assert.IsFalse(jschema.Items.IsArray);
            Assert.AreEqual(null, jschema.Items.Array);
        }
        [TestMethod]
        public void Items_SetEmptyObject_IsEmptyJSchemaNotArray()
        {
            JSchema jschema = JSchema.Parse(@"{'items':{}}");

            Assert.IsTrue(jschema.Items.IsSchema);
            Assert.AreEqual(new JSchema(), jschema.Items.Schema);
            Assert.IsFalse(jschema.Items.IsArray);
            Assert.AreEqual(null, jschema.Items.Array);
        }
        [TestMethod]
        public void Items_SetEmptyArray_IsEmptyArrayNotSchema()
        {
            JSchema jschema = JSchema.Parse(@"{'items':[]}");

            Assert.IsFalse(jschema.Items.IsSchema);
            Assert.AreEqual(null, jschema.Items.Schema);
            Assert.IsTrue(jschema.Items.IsArray);
            Assert.AreNotEqual(null, jschema.Items.Array);
            Assert.AreEqual(0, jschema.Items.Array.Count);
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
        #endregion
    }
}
