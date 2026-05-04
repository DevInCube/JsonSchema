using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text.Json.Nodes;

namespace My.Json.Schema.Tests;

[TestClass]
[System.Diagnostics.CodeAnalysis.SuppressMessage("Maintainability", "CA1515:Consider making public types internal")]
public class JSchemaValidationTests
{
    // bignum
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

    // multipleOf
    [TestMethod]
    public void MultipleOf_ValidInteger_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""multipleOf"":2}");
        JsonNode data = JsonNode.Parse("10");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MultipleOf_NotMultiple_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""multipleOf"":2}");
        JsonNode data = JsonNode.Parse("7");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void MultipleOf_FloatMultiple_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""multipleOf"":1.5}");
        JsonNode data = JsonNode.Parse("3.0");
        Assert.IsTrue(data.IsValid(schema));
    }

    // maximum / exclusiveMaximum
    [TestMethod]
    public void Maximum_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""maximum"":5}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Maximum_AboveBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""maximum"":5}");
        JsonNode data = JsonNode.Parse("6");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Maximum_ExclusiveMaximumAtBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Maximum_ExclusiveMaximumBelowBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""maximum"":5,""exclusiveMaximum"":true}");
        JsonNode data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    // minimum / exclusiveMinimum
    [TestMethod]
    public void Minimum_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""minimum"":3}");
        JsonNode data = JsonNode.Parse("3");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Minimum_BelowBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""minimum"":3}");
        JsonNode data = JsonNode.Parse("2");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Minimum_ExclusiveMinimumAtBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        JsonNode data = JsonNode.Parse("3");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Minimum_ExclusiveMinimumAboveBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""minimum"":3,""exclusiveMinimum"":true}");
        JsonNode data = JsonNode.Parse("4");
        Assert.IsTrue(data.IsValid(schema));
    }

    // maxLength / minLength
    [TestMethod]
    public void MaxLength_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxLength"":3}");
        JsonNode data = JsonNode.Parse(@"""abc""");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MaxLength_Exceeded_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxLength"":3}");
        JsonNode data = JsonNode.Parse(@"""abcd""");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void MinLength_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""minLength"":3}");
        JsonNode data = JsonNode.Parse(@"""abc""");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MinLength_BelowBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""minLength"":3}");
        JsonNode data = JsonNode.Parse(@"""ab""");
        Assert.IsFalse(data.IsValid(schema));
    }

    // pattern
    [TestMethod]
    public void Pattern_Matches_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""pattern"":""^[a-z]+$""}");
        JsonNode data = JsonNode.Parse(@"""hello""");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Pattern_NoMatch_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""pattern"":""^[a-z]+$""}");
        JsonNode data = JsonNode.Parse(@"""Hello123""");
        Assert.IsFalse(data.IsValid(schema));
    }

    // items (schema form)
    [TestMethod]
    public void Items_AllMatch_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""items"":{""type"":""integer""}}");
        JsonNode data = JsonNode.Parse("[1,2,3]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Items_OneMismatch_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""items"":{""type"":""integer""}}");
        JsonNode data = JsonNode.Parse(@"[1,""two"",3]");
        Assert.IsFalse(data.IsValid(schema));
    }

    // items (array form) + additionalItems
    [TestMethod]
    public void ItemsArray_MatchesTuplePositions_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""items"":[{""type"":""integer""},{""type"":""string""}]}");
        JsonNode data = JsonNode.Parse(@"[1,""a""]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void ItemsArray_AdditionalItemsNotAllowed_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""items"":[{""type"":""integer""}],""additionalItems"":false}");
        JsonNode data = JsonNode.Parse("[1,2]");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void ItemsArray_AdditionalItemsAllowed_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""items"":[{""type"":""integer""}],""additionalItems"":true}");
        JsonNode data = JsonNode.Parse("[1,2,3]");
        Assert.IsTrue(data.IsValid(schema));
    }

    // maxItems / minItems
    [TestMethod]
    public void MaxItems_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxItems"":3}");
        JsonNode data = JsonNode.Parse("[1,2,3]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MaxItems_Exceeded_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxItems"":3}");
        JsonNode data = JsonNode.Parse("[1,2,3,4]");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void MinItems_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""minItems"":2}");
        JsonNode data = JsonNode.Parse("[1,2]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MinItems_BelowBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""minItems"":2}");
        JsonNode data = JsonNode.Parse("[1]");
        Assert.IsFalse(data.IsValid(schema));
    }

    // uniqueItems
    [TestMethod]
    public void UniqueItems_AllUnique_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""uniqueItems"":true}");
        JsonNode data = JsonNode.Parse("[1,2,3]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void UniqueItems_Duplicate_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""uniqueItems"":true}");
        JsonNode data = JsonNode.Parse("[1,2,1]");
        Assert.IsFalse(data.IsValid(schema));
    }

    // maxProperties / minProperties
    [TestMethod]
    public void MaxProperties_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxProperties"":2}");
        JsonNode data = JsonNode.Parse(@"{""a"":1,""b"":2}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MaxProperties_Exceeded_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""maxProperties"":2}");
        JsonNode data = JsonNode.Parse(@"{""a"":1,""b"":2,""c"":3}");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void MinProperties_AtBoundary_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""minProperties"":2}");
        JsonNode data = JsonNode.Parse(@"{""a"":1,""b"":2}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void MinProperties_BelowBoundary_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""minProperties"":2}");
        JsonNode data = JsonNode.Parse(@"{""a"":1}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // required
    [TestMethod]
    public void Required_PropertyPresent_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""required"":[""name""]}");
        JsonNode data = JsonNode.Parse(@"{""name"":""Alice""}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Required_PropertyMissing_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""required"":[""name""]}");
        JsonNode data = JsonNode.Parse(@"{""age"":30}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // additionalProperties
    [TestMethod]
    public void AdditionalProperties_NotAllowed_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""properties"":{""a"":{}},""additionalProperties"":false}");
        JsonNode data = JsonNode.Parse(@"{""a"":1,""b"":2}");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void AdditionalProperties_NotAllowed_KnownPropertyOnly_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""properties"":{""a"":{}},""additionalProperties"":false}");
        JsonNode data = JsonNode.Parse(@"{""a"":1}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void AdditionalProperties_SchemaAllowed_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""additionalProperties"":{""type"":""integer""}}");
        JsonNode data = JsonNode.Parse(@"{""x"":1,""y"":2}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void AdditionalProperties_SchemaMismatch_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""additionalProperties"":{""type"":""integer""}}");
        JsonNode data = JsonNode.Parse(@"{""x"":""not-an-int""}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // enum
    [TestMethod]
    public void Enum_MatchingValue_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""enum"":[1,2,3]}");
        JsonNode data = JsonNode.Parse("2");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Enum_NonMatchingValue_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""enum"":[1,2,3]}");
        JsonNode data = JsonNode.Parse("4");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Enum_NullValue_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""enum"":[null,1]}");
        JsonNode data = JsonNode.Parse("null");
        Assert.IsTrue(data.IsValid(schema));
    }

    // type
    [TestMethod]
    public void Type_String_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""string""}");
        JsonNode data = JsonNode.Parse(@"""hello""");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_String_NumberGiven_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""string""}");
        JsonNode data = JsonNode.Parse("1");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Number_IntegerGiven_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""number""}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Integer_FloatGiven_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""integer""}");
        JsonNode data = JsonNode.Parse("1.5");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Boolean_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""boolean""}");
        JsonNode data = JsonNode.Parse("true");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Null_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""null""}");
        JsonNode data = JsonNode.Parse("null");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Array_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""array""}");
        JsonNode data = JsonNode.Parse("[1,2]");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_Object_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":""object""}");
        JsonNode data = JsonNode.Parse(@"{""a"":1}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_MultipleTypes_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":[""string"",""integer""]}");
        JsonNode data = JsonNode.Parse("42");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Type_MultipleTypes_NeitherMatch_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""type"":[""string"",""integer""]}");
        JsonNode data = JsonNode.Parse("true");
        Assert.IsFalse(data.IsValid(schema));
    }

    // allOf
    [TestMethod]
    public void AllOf_AllSatisfied_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""allOf"":[{""minimum"":1},{""maximum"":10}]}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void AllOf_OneFails_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""allOf"":[{""minimum"":1},{""maximum"":10}]}");
        JsonNode data = JsonNode.Parse("11");
        Assert.IsFalse(data.IsValid(schema));
    }

    // anyOf
    [TestMethod]
    public void AnyOf_OneSatisfied_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""anyOf"":[{""type"":""string""},{""type"":""integer""}]}");
        JsonNode data = JsonNode.Parse("1");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void AnyOf_NoneSatisfied_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""anyOf"":[{""type"":""string""},{""type"":""integer""}]}");
        JsonNode data = JsonNode.Parse("true");
        Assert.IsFalse(data.IsValid(schema));
    }

    // oneOf
    [TestMethod]
    public void OneOf_ExactlyOneSatisfied_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""oneOf"":[{""minimum"":1},{""maximum"":0}]}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void OneOf_NoneSatisfied_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""oneOf"":[{""minimum"":10},{""minimum"":20}]}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void OneOf_BothSatisfied_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""oneOf"":[{""minimum"":1},{""maximum"":10}]}");
        JsonNode data = JsonNode.Parse("5");
        Assert.IsFalse(data.IsValid(schema));
    }

    // not
    [TestMethod]
    public void Not_SchemaNotSatisfied_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""not"":{""type"":""string""}}");
        JsonNode data = JsonNode.Parse("42");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Not_SchemaSatisfied_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""not"":{""type"":""string""}}");
        JsonNode data = JsonNode.Parse(@"""hello""");
        Assert.IsFalse(data.IsValid(schema));
    }

    // properties
    [TestMethod]
    public void Properties_ValidValue_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""properties"":{""age"":{""type"":""integer"",""minimum"":0}}}");
        JsonNode data = JsonNode.Parse(@"{""age"":25}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void Properties_InvalidValue_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""properties"":{""age"":{""type"":""integer"",""minimum"":0}}}");
        JsonNode data = JsonNode.Parse(@"{""age"":-1}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // patternProperties
    [TestMethod]
    public void PatternProperties_MatchingKeyValidValue_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""patternProperties"":{""^S_"":{""type"":""string""}}}");
        JsonNode data = JsonNode.Parse(@"{""S_name"":""Alice""}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void PatternProperties_MatchingKeyInvalidValue_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""patternProperties"":{""^S_"":{""type"":""string""}}}");
        JsonNode data = JsonNode.Parse(@"{""S_count"":42}");
        Assert.IsFalse(data.IsValid(schema));
    }

    // dependencies (property)
    [TestMethod]
    public void PropertyDependency_DependentPresent_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""dependencies"":{""credit_card"":[""billing_address""]}}");
        JsonNode data = JsonNode.Parse(@"{""credit_card"":""1234"",""billing_address"":""123 Main St""}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void PropertyDependency_DependentMissing_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""dependencies"":{""credit_card"":[""billing_address""]}}");
        JsonNode data = JsonNode.Parse(@"{""credit_card"":""1234""}");
        Assert.IsFalse(data.IsValid(schema));
    }

    [TestMethod]
    public void PropertyDependency_TriggerAbsent_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""dependencies"":{""credit_card"":[""billing_address""]}}");
        JsonNode data = JsonNode.Parse(@"{""name"":""Alice""}");
        Assert.IsTrue(data.IsValid(schema));
    }

    // dependencies (schema)
    [TestMethod]
    public void SchemaDependency_TriggerPresent_Satisfies_IsValid()
    {
        JSchema schema = JSchema.Parse(@"{""dependencies"":{""name"":{""required"":[""age""]}}}");
        JsonNode data = JsonNode.Parse(@"{""name"":""Alice"",""age"":30}");
        Assert.IsTrue(data.IsValid(schema));
    }

    [TestMethod]
    public void SchemaDependency_TriggerPresent_NotSatisfies_IsNotValid()
    {
        JSchema schema = JSchema.Parse(@"{""dependencies"":{""name"":{""required"":[""age""]}}}");
        JsonNode data = JsonNode.Parse(@"{""name"":""Alice""}");
        Assert.IsFalse(data.IsValid(schema));
    }
}
