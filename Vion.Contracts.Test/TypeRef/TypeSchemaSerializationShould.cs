using System.Collections.Immutable;
using System.Text.Json.Nodes;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.TypeRef
{
    [TestClass]
    public class TypeSchemaSerializationShould
    {
        [TestMethod]
        [DataRow(PrimitiveKind.Bool, "{\"type\":\"boolean\"}")]
        [DataRow(PrimitiveKind.String, "{\"type\":\"string\"}")]
        [DataRow(PrimitiveKind.Byte, "{\"type\":\"integer\",\"format\":\"uint8\"}")]
        [DataRow(PrimitiveKind.Short, "{\"type\":\"integer\",\"format\":\"int16\"}")]
        [DataRow(PrimitiveKind.UShort, "{\"type\":\"integer\",\"format\":\"uint16\"}")]
        [DataRow(PrimitiveKind.Int, "{\"type\":\"integer\",\"format\":\"int32\"}")]
        [DataRow(PrimitiveKind.UInt, "{\"type\":\"integer\",\"format\":\"uint32\"}")]
        [DataRow(PrimitiveKind.Long, "{\"type\":\"integer\",\"format\":\"int64\"}")]
        [DataRow(PrimitiveKind.Float, "{\"type\":\"number\",\"format\":\"float\"}")]
        [DataRow(PrimitiveKind.Double, "{\"type\":\"number\",\"format\":\"double\"}")]
        [DataRow(PrimitiveKind.DateTime, "{\"type\":\"string\",\"format\":\"date-time\"}")]
        [DataRow(PrimitiveKind.Duration, "{\"type\":\"string\",\"format\":\"duration\"}")]
        public void EmitJsonSchemaForPrimitiveKind(PrimitiveKind kind, string expected)
        {
            var schema = TypeSchema.Of(new PrimitiveTypeRef(kind));
            var actual = schema.ToJsonSchema();

            // Compare via canonical-JSON string form (JsonNode.Equals is reference-based).
            var expectedCanonical = JsonNode.Parse(expected)!.ToJsonString();
            Assert.AreEqual(expectedCanonical, actual.ToJsonString());
        }

        [TestMethod]
        public void EmitJsonSchemaForEnumWithMultipleMembers()
        {
            var schema = TypeSchema.Of(new EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"AlarmState\",\"enum\":[\"Ok\",\"Warning\",\"Critical\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForEnumWithSingleMember()
        {
            var schema = TypeSchema.Of(new EnumTypeRef("Mode", ImmutableArray.Create("Auto")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"Mode\",\"enum\":[\"Auto\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreserveEnumMemberDeclarationOrderInJsonSchema()
        {
            // Same members, different declaration order — JSON output must reflect the input order.
            var ascending = TypeSchema.Of(new EnumTypeRef("Severity", ImmutableArray.Create("Low", "Medium", "High")));
            var descending = TypeSchema.Of(new EnumTypeRef("Severity", ImmutableArray.Create("High", "Medium", "Low")));

            var ascJson = ascending.ToJsonSchema().ToJsonString();
            var descJson = descending.ToJsonSchema().ToJsonString();

            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"Low\",\"Medium\",\"High\"]}", ascJson);
            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"High\",\"Medium\",\"Low\"]}", descJson);
            Assert.AreNotEqual(ascJson, descJson);
        }

        [TestMethod]
        public void EmitJsonSchemaForStructWithPrimitiveFields()
        {
            var s = new StructTypeRef("Coordinates",
                                      ImmutableArray.Create(new StructField("lat", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("lon", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("lat", "lon"));
            var schema = TypeSchema.Of(s);
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"object\"," + "\"title\":\"Coordinates\"," + "\"properties\":{" + "\"lat\":{\"type\":\"number\",\"format\":\"double\"}," +
                           "\"lon\":{\"type\":\"number\",\"format\":\"double\"}" + "}," + "\"required\":[\"lat\",\"lon\"]," + "\"additionalProperties\":false}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreserveStructFieldDeclarationOrderInJsonSchema()
        {
            var ascending = new StructTypeRef("Pair",
                                              ImmutableArray.Create(new StructField("a", new PrimitiveTypeRef(PrimitiveKind.Int)),
                                                                    new StructField("b", new PrimitiveTypeRef(PrimitiveKind.Int))),
                                              ImmutableArray.Create("a", "b"));
            var descending = new StructTypeRef("Pair",
                                               ImmutableArray.Create(new StructField("b", new PrimitiveTypeRef(PrimitiveKind.Int)),
                                                                     new StructField("a", new PrimitiveTypeRef(PrimitiveKind.Int))),
                                               ImmutableArray.Create("b", "a"));

            var ascJson = TypeSchema.Of(ascending).ToJsonSchema().ToJsonString();
            var descJson = TypeSchema.Of(descending).ToJsonSchema().ToJsonString();

            // Properties object key order, plus required-array element order, both differ.
            Assert.AreNotEqual(ascJson, descJson);
            StringAssert.Contains(ascJson, "\"properties\":{\"a\":{");
            StringAssert.Contains(descJson, "\"properties\":{\"b\":{");
            StringAssert.Contains(ascJson, "\"required\":[\"a\",\"b\"]");
            StringAssert.Contains(descJson, "\"required\":[\"b\",\"a\"]");
        }

        [TestMethod]
        public void EmitRequiredArraySubsetWhenSomeFieldsOptional()
        {
            var s = new StructTypeRef("PartialPoint",
                                      ImmutableArray.Create(new StructField("x", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("y", new PrimitiveTypeRef(PrimitiveKind.Double)),
                                                            new StructField("z", new PrimitiveTypeRef(PrimitiveKind.Double))),
                                      ImmutableArray.Create("x", "y")); // z is optional
            var actual = TypeSchema.Of(s).ToJsonSchema().ToJsonString();

            StringAssert.Contains(actual, "\"properties\":{\"x\":{");
            StringAssert.Contains(actual, "\"y\":{");
            StringAssert.Contains(actual, "\"z\":{");
            StringAssert.Contains(actual, "\"required\":[\"x\",\"y\"]");
            Assert.IsFalse(actual.Contains("\"required\":[\"x\",\"y\",\"z\"]"));
        }
    }
}