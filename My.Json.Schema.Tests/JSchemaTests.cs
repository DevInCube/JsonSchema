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

            Assert.AreEqual(null, jschema.Id, "id");
            Assert.AreEqual(null, jschema.Title, "Title");
            Assert.AreEqual(null, jschema.Description, "Description");
            Assert.AreEqual(null, jschema.Default, "Default");
            Assert.AreEqual(null, jschema.Format, "Format");
            Assert.AreEqual(JSchemaType.None, jschema.Type, "Type");

            Assert.AreNotEqual(null, jschema.ItemsSchema, "ItemsSchema");
            Assert.AreNotEqual(null, jschema.ItemsArray, "ItemsArray");
            Assert.AreEqual(0, jschema.ItemsArray.Count, "ItemsArray.Count");

            Assert.AreNotEqual(null, jschema.Properties, "Properties");
            Assert.AreEqual(0, jschema.Properties.Count, "Properties.Count");
            Assert.AreEqual(null, jschema.MultipleOf, "MultipleOf");
            Assert.AreEqual(null, jschema.Maximum, "Maximum");
            Assert.AreEqual(null, jschema.Minimum, "Minimum");
            Assert.AreEqual(null, jschema.MaxLength, "MaxLength");
            Assert.AreEqual(null, jschema.MinLength, "MinLength");
            Assert.AreEqual(null, jschema.MinItems, "MinItems");
            Assert.AreEqual(null, jschema.MaxItems, "MaxItems");
            Assert.IsFalse(jschema.UniqueItems, "UniqueItems");
            Assert.AreNotEqual(null, jschema.Required, "Required");
            Assert.AreEqual(0, jschema.Required.Count, "Required.Count");
            Assert.AreNotEqual(null, jschema.Enum, "Enum");
            Assert.AreEqual(0, jschema.Enum.Count, "Enum");
            Assert.IsTrue(jschema.AllowAdditionalProperties, "AllowAdditionalProperties");
            Assert.AreNotEqual(null, jschema.PatternProperties, "PatternProperties");
            Assert.AreEqual(0, jschema.PatternProperties.Count, "PatternProperties.Count");
            Assert.AreNotEqual(null, jschema.SchemaDependencies, "SchemaDependencies");
            Assert.AreEqual(0, jschema.SchemaDependencies.Count, "SchemaDependencies.Count");
            Assert.AreNotEqual(null, jschema.PropertyDependencies, "PropertyDependencies");
            Assert.AreEqual(0, jschema.PropertyDependencies.Count, "PropertyDependencies.Count");  
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
            Assert.AreEqual(new Uri("http://x.y.z/rootschema.json#"), jschema.Id);
        }
        [TestMethod]
        public void Id_SetAsString_IsValidAndMatches()
        {
            JSchema jschema = JSchema.Parse(@"{id:'stringId'}");
            Assert.AreEqual(new Uri("stringId", UriKind.Relative), jschema.Id);
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Id_SetAsObject_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{id:{}}");            
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Id_SetAsEmptyFragment_ThrowsError()
        {
            JSchema jschema = new JSchema();
            jschema.Id = new Uri("#", UriKind.Relative);
        }
        [TestMethod]
        public void Id_AlterResolutionScope_IsValidAndMatches()
        {
            string schema = @"{
    'id': 'http://x.y.z/rootschema.json#',
    'definitions' : {
        'schema1': {
            'id': '#foo'
        },
        'schema2': {
            'id': 'otherschema.json',
            'definitions' : {
                'nested': {
                    'id': '#bar'
                },
                'alsonested': {
                    'id': 't/inner.json#a'
                }
            }
        },
        'schema3': {
            'id': 'some://where.else/completely#'
        },
    },
    'properties' : {
        'test' : { '$ref' : 'otherschema.json#bar' }
    }
}";
            JSchema jschema = JSchema.Parse(schema);

            Assert.AreEqual(new Uri("#bar", UriKind.Relative), jschema.Properties["test"].Id);
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
        [ExpectedException(typeof(JSchemaException))]
        public void Type_SetNotUniqueArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'type':['object','object']}");
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
        public void Reference_ResolveWithinDefinitions_ReferenceResolvedAndHasTypeString()
        {
            JSchema jschema = JSchema.Parse(@"{
    'definitions':{
        'test':{'type':'string'},
        'test2':{ '$ref':'test' },
    },
    'properties' : { 'refTest' : {'$ref' : '#/definitions/test2'}},
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
        [TestMethod]
        public void Ref_SetExternalReferenceWithScopeChanged_ReferenceResolvedAndHasTypeString()
        {
            var mock = new Mock<JSchemaResolver>();
            mock.Setup(ins => ins.GetSchemaResource(new Uri("http://localhost:1234/folder/test.json")))
                .Returns(new MemoryStream(
                    Encoding.UTF8.GetBytes("{ 'type' : 'string' }")));

            JSchema jschema = JSchema.Parse(@" {
    'id': 'http://localhost:1234/',
    'items': {
        'id': 'folder/',
        'items': {'$ref': 'test.json'}
    }
}", mock.Object);
            var sh = jschema.ItemsSchema.ItemsSchema;
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.String));
        }

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

        [TestMethod]
        public void Reference_InlineDereferencingReverseOrder_OK()
        {
            string shStr = @"{
    'id': 'http://some.site/schema#',
    'not': { '$ref': '#inner' },
    'definitions': {
        'schema1': {
            'id': '#inner',
            'type': 'boolean'
        }
    }
}";
            JSchema jschema = JSchema.Parse(shStr);
            var sh = jschema.Not;
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
        }

        [TestMethod]
        public void Reference_InlineDereferencingWithoutBaseUri_OK()
        {
            string shStr = @"{    
    'not': { '$ref': '#inner' },
    'definitions': {
        'schema1': {
            'id': '#inner',
            'type': 'boolean'
        }
    }
}";
            JSchema jschema = JSchema.Parse(shStr);
            var sh = jschema.Not;
            Assert.IsTrue(sh.Type.HasFlag(JSchemaType.Boolean));
        }

        [TestMethod]
        public void Reference_SubschemaDiscovery_OK()
        {
            string shStr = @"{    
    'not': { '$ref': '#/inner' },
    'additionalProperties': { '$ref': '#/inner/schema1' },
    'inner': {
        'title' : 'ok',
        'schema1': {
            'title' : 'ok/ok',
            'id': '#inner',
            'type': 'boolean'
        }
    }
}";
            JSchema jschema = JSchema.Parse(shStr);
            Assert.AreEqual("ok", jschema.Not.Title);
            Assert.AreEqual("ok/ok", jschema.AdditionalProperties.Title);
        }

        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void Reference_NonExistingSubschemaDiscovery_ThrowError()
        {
            string shStr = @"{    
    'not': { '$ref': '#/inner' },    
}";
            JSchema jschema = JSchema.Parse(shStr);
        }        


        [TestMethod]
        public void Reference_Loop_PointersEquals()
        {
            string shStr = @"{ 'properties': { 'loop': { '$ref' : '#' } }}";
            JSchema jschema = JSchema.Parse(shStr);
            var loop = jschema.Properties["loop"];
            Assert.AreEqual(jschema, loop);
        }      

        #endregion

        #region items_tests
        [TestMethod]
        public void Items_ParseAsSchema_SchemaMatches()
        {
            string shStr = @"{ 'items': { 'type':'integer' }}";
            JSchema jschema = JSchema.Parse(shStr);
            Assert.IsTrue(jschema.ItemsSchema.Type.HasFlag(JSchemaType.Integer));            
        }
        [TestMethod]
        public void Items_ParseAsList_SchemasMatch()
        {
            string shStr = @"{ 'items': [{ 'type':'integer' },{ 'type':'boolean' } ]}";
            JSchema jschema = JSchema.Parse(shStr);

            Assert.AreEqual(2, jschema.ItemsArray.Count);
            Assert.IsTrue(jschema.ItemsArray[0].Type.HasFlag(JSchemaType.Integer));
            Assert.IsTrue(jschema.ItemsArray[1].Type.HasFlag(JSchemaType.Boolean));
        }    
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
        public void ExclusiveMaximum_ParseIsSetButNoMaximum_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'exclusiveMaximum':true}");
        }
        [TestMethod]
        public void ExclusiveMaximum_ParseIsSetTrue_IsTrue()
        {
            JSchema jschema = JSchema.Parse(@"{'maximum':1, 'exclusiveMaximum':true}");

            Assert.IsTrue(jschema.ExclusiveMaximum);
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

        #region additionalProperties
        [TestMethod]
        public void AdditionalProperties_ParseAsEmptyObject_OK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalProperties:{}}");

            Assert.IsTrue(JToken.DeepEquals(new JObject(), JObject.Parse(jschema.AdditionalProperties.ToString())));
        }        
        #endregion

        #region AllowAdditionalItems
        [TestMethod]
        public void AllowAdditionalItems_ParseAsFalseBool_OK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalItems:false}");

            Assert.IsFalse(jschema.AllowAdditionalItems);
        }
        [TestMethod]
        public void AllowAdditionalItems_ParseAsTrueBool_OK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalItems:true}");

            Assert.IsTrue(jschema.AllowAdditionalItems);
        }
        #endregion

        #region AdditionalItems
        [TestMethod]
        public void AdditionalItems_SetEmptySchema_TypeOK()
        {
            JSchema jschema = JSchema.Parse(@"{additionalItems:{'type':'boolean'}}");

            Assert.IsTrue(jschema.AdditionalItems.Type.HasFlag(JSchemaType.Boolean));
        }
        #endregion

        #region dependencies
        #endregion

        #region allOf
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void allOf_ParseAsObject_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'allOf':{}}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void allOf_ParseAsEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'allOf':[]}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]       
        public void allOf_ParseAsOneItemStringArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'allOf':['string']}");
        }
        [TestMethod]        
        public void allOf_ParseAsOneItemObjectArray_MatchesSchema()
        {
            JSchema jschema = JSchema.Parse(@"{'allOf':[{}]}");

            Assert.IsTrue(JToken.DeepEquals(new JObject(), JObject.Parse(jschema.AllOf[0].ToString())));
        }
        #endregion

        #region anyOf
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void anyOf_ParseAsObject_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'anyOf':{}}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void anyOf_ParseAsEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'anyOf':[]}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void anyOf_ParseAsOneItemStringArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'anyOf':['string']}");
        }
        [TestMethod]
        public void anyOf_ParseAsOneItemObjectArray_MatchesSchema()
        {
            JSchema jschema = JSchema.Parse(@"{'anyOf':[{}]}");

            Assert.IsTrue(JToken.DeepEquals(new JObject(), JObject.Parse(jschema.AnyOf[0].ToString())));
        }
        #endregion

        #region oneOf
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void oneOf_ParseAsObject_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'oneOf':{}}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void oneOf_ParseAsEmptyArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'oneOf':[]}");
        }
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void oneOf_ParseAsOneItemStringArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'oneOf':['string']}");
        }
        [TestMethod]
        public void oneOf_ParseAsOneItemObjectArray_MatchesSchema()
        {
            JSchema jschema = JSchema.Parse(@"{'oneOf':[{}]}");

            Assert.IsTrue(JToken.DeepEquals(new JObject(), JObject.Parse(jschema.OneOf[0].ToString())));
        }
        #endregion

        #region not
        [TestMethod]
        [ExpectedException(typeof(JSchemaException))]
        public void not_ParseAsArray_ThrowsError()
        {
            JSchema jschema = JSchema.Parse(@"{'not':[]}");
        }
        [TestMethod]        
        public void not_ParseAsEmptyObject_Match()
        {
            JSchema jschema = JSchema.Parse(@"{'not':{}}");

            Assert.IsTrue(JToken.DeepEquals(new JObject(), JObject.Parse(jschema.Not.ToString())));
        }
        #endregion

        #region ExtensionData
        [TestMethod]
        public void ExtensionData_ParseDefinitions_IsAddedToExtensionData()
        {
            JSchema jschema = JSchema.Parse(@"{'definitions':{}}");

            Assert.IsTrue(jschema.ExtensionData.ContainsKey("definitions"));
            Assert.IsTrue(JToken.DeepEquals(new JObject(), jschema.ExtensionData["definitions"]));
        }
        [TestMethod]
        public void ExtensionData_ParseNotAKeyword_IsAddedToExtensionData()
        {
            JSchema jschema = JSchema.Parse(@"{'ext':{}}");

            Assert.IsTrue(jschema.ExtensionData.ContainsKey("ext"));
            Assert.IsTrue(JToken.DeepEquals(new JObject(), jschema.ExtensionData["ext"]));
        }
        #endregion
    }
}
