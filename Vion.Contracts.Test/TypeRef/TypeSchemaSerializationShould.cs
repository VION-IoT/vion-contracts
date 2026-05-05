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
    }
}