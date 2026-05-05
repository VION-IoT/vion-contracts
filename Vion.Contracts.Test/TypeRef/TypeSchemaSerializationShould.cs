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
            var schema = TypeSchema.Of(new EnumTypeRef("AlarmState", System.Collections.Immutable.ImmutableArray.Create("Ok", "Warning", "Critical")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"AlarmState\",\"enum\":[\"Ok\",\"Warning\",\"Critical\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void EmitJsonSchemaForEnumWithSingleMember()
        {
            var schema = TypeSchema.Of(new EnumTypeRef("Mode", System.Collections.Immutable.ImmutableArray.Create("Auto")));
            var actual = schema.ToJsonSchema().ToJsonString();

            var expected = "{\"type\":\"string\",\"title\":\"Mode\",\"enum\":[\"Auto\"]}";
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void PreserveEnumMemberDeclarationOrderInJsonSchema()
        {
            // Same members, different declaration order — JSON output must reflect the input order.
            var ascending = TypeSchema.Of(new EnumTypeRef("Severity", System.Collections.Immutable.ImmutableArray.Create("Low", "Medium", "High")));
            var descending = TypeSchema.Of(new EnumTypeRef("Severity", System.Collections.Immutable.ImmutableArray.Create("High", "Medium", "Low")));

            var ascJson = ascending.ToJsonSchema().ToJsonString();
            var descJson = descending.ToJsonSchema().ToJsonString();

            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"Low\",\"Medium\",\"High\"]}", ascJson);
            Assert.AreEqual("{\"type\":\"string\",\"title\":\"Severity\",\"enum\":[\"High\",\"Medium\",\"Low\"]}", descJson);
            Assert.AreNotEqual(ascJson, descJson);
        }
    }
}