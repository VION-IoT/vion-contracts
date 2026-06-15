using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using Vion.Contracts.Codec;
using TR = Vion.Contracts.TypeRef;

namespace Vion.Contracts.Test.Codec
{
    public enum AlarmState
    {
        Ok,

        Warning,

        Critical,
    }

    public readonly record struct Coordinates(double Lat, double Lon);

    public readonly record struct Coordinates3D(double Lat, double Lon, double Altitude);

    [TestClass]
    public class PropertyValueCodecShould
    {
        // ── JsonToClr direct-entry tests ─────────────────────────────────────
        // Assert the public direct-entry semantics: decode a pre-parsed JsonNode
        // into a CLR value via the declared schema (REST APIs, dev tooling, mocks).

        [TestMethod]
        public void JsonToClrConvertsPrimitiveDirectly()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double);
            var json = JsonValue.Create(3.14);
            var result = PropertyValueCodec.JsonToClr(json, schema, typeof(double));
            Assert.AreEqual(3.14, (double)result!);
        }

        [TestMethod]
        public void JsonToClrConvertsNullableNullDirectly()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var result = PropertyValueCodec.JsonToClr(null, schema, typeof(double?));
            Assert.IsNull(result);
        }

        [TestMethod]
        public void JsonToClrConvertsEnumNameDirectly()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var json = JsonValue.Create("Warning");
            var result = PropertyValueCodec.JsonToClr(json, schema, typeof(AlarmState));
            Assert.AreEqual(AlarmState.Warning, result);
        }

        [TestMethod]
        public void JsonToClrConvertsStructDirectly()
        {
            var schema = new TR.StructTypeRef("Coordinates",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon"));
            var json = new JsonObject { ["lat"] = 47.3, ["lon"] = 8.5 };
            var result = (Coordinates)PropertyValueCodec.JsonToClr(json, schema, typeof(Coordinates))!;
            Assert.AreEqual(47.3, result.Lat);
            Assert.AreEqual(8.5, result.Lon);
        }

        [TestMethod]
        public void JsonToClrConvertsImmutableArrayDirectly()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var json = JsonNode.Parse("[1,2,3]");
            var result = (ImmutableArray<long>)PropertyValueCodec.JsonToClr(json, schema, typeof(ImmutableArray<long>))!;
            Assert.HasCount(3, result);
            Assert.AreEqual(1L, result[0]);
            Assert.AreEqual(2L, result[1]);
            Assert.AreEqual(3L, result[2]);
        }

        [TestMethod]
        public void JsonToClrThrowsOnNullValueForNonNullableSchema()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double);
            var ex = Assert.ThrowsExactly<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToClr(null, schema, typeof(double)));
            StringAssert.Contains(ex.Message, "JsonToClr");
        }

        [TestMethod]
        public void JsonToClrByteOutOfRangeThrows()
        {
            var byteSchema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Byte);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToClr(JsonValue.Create(300L), byteSchema, typeof(byte)));
        }

        [TestMethod]
        public void JsonToClrUShortOutOfRangeThrows()
        {
            var ushortSchema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.UShort);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToClr(JsonValue.Create(70000L), ushortSchema, typeof(ushort)));
        }

        [TestMethod]
        public void JsonToClrUIntNegativeThrows()
        {
            var uintSchema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.UInt);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToClr(JsonValue.Create(-1L), uintSchema, typeof(uint)));
        }

        [TestMethod]
        public void JsonToClrUnknownEnumNameThrows()
        {
            var enumSchema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToClr(JsonValue.Create("NotAMember"), enumSchema, typeof(AlarmState)));
        }

        // ── ClrToJson direct-entry tests ─────────────────────────────────────
        // Assert the public direct-entry semantics: encode a CLR value into a
        // JsonNode via the declared schema.

        [TestMethod]
        public void ClrToJsonConvertsPrimitiveDirectly()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double);
            var json = PropertyValueCodec.ClrToJson(3.14, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(3.14, json!.GetValue<double>(), 1e-9);
        }

        [TestMethod]
        public void ClrToJsonConvertsEnumToMemberName()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var json = PropertyValueCodec.ClrToJson(AlarmState.Warning, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual("Warning", json!.GetValue<string>());
        }

        [TestMethod]
        public void ClrToJsonConvertsStructToCamelCaseObject()
        {
            var schema = new TR.StructTypeRef("Coordinates",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon"));
            var json = PropertyValueCodec.ClrToJson(new Coordinates(47.3, 8.5), schema);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.AreEqual(47.3, obj["lat"]!.GetValue<double>());
            Assert.AreEqual(8.5, obj["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void ClrToJsonNullableNullWritesJsonNull()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var json = PropertyValueCodec.ClrToJson(null, schema);
            Assert.IsNull(json);
        }

        [TestMethod]
        public void ClrToJsonLocalDateTimeThrows()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime);
            var localDt = System.DateTime.SpecifyKind(new System.DateTime(2024,
                                                                          1,
                                                                          1,
                                                                          0,
                                                                          0,
                                                                          0),
                                                      System.DateTimeKind.Local);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToJson(localDt, schema));
        }

        [TestMethod]
        public void ClrToJsonNullForNonNullablePrimitiveThrows()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToJson(null, schema));
        }

        // ── ValidateJson tests ────────────────────────────────────────────────

        [TestMethod]
        public void ValidateValidDouble()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(5.0), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateMismatchedPrimitiveType()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create("hi"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("expected JSON number")));
        }

        [TestMethod]
        public void ValidateMinimumViolation()
        {
            var schema = new TR.TypeSchema(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double),
                                           new TR.TypeAnnotations { Minimum = 0 },
                                           ImmutableDictionary<string, TR.TypeAnnotations>.Empty);
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(-1.0), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("minimum")));
        }

        [TestMethod]
        public void ValidateMaximumViolation()
        {
            var schema = new TR.TypeSchema(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double),
                                           new TR.TypeAnnotations { Maximum = 100 },
                                           ImmutableDictionary<string, TR.TypeAnnotations>.Empty);
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(101.0), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("maximum")));
        }

        [TestMethod]
        public void ValidateInRange()
        {
            var schema = new TR.TypeSchema(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double),
                                           new TR.TypeAnnotations { Minimum = 0, Maximum = 100 },
                                           ImmutableDictionary<string, TR.TypeAnnotations>.Empty);
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(50.0), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateIntegerOutOfRangeForByte()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Byte));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(300L), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("out of range")));
        }

        [TestMethod]
        public void ValidateValidDateTimeString()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create("2024-06-03T12:00:00Z"), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateInvalidDateTimeString()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create("not-a-date"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.HasCount(1, result.Errors);
        }

        [TestMethod]
        public void ValidateEnumMember()
        {
            var schema = TR.TypeSchema.Of(new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create("Warning"), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateNonEnumMember()
        {
            var schema = TR.TypeSchema.Of(new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create("Unknown"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("not a member")));
        }

        [TestMethod]
        public void ValidateStructAllRequiredPresent()
        {
            var schema = TR.TypeSchema.Of(new TR.StructTypeRef("Coordinates",
                                                               ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                                     new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                               ImmutableArray.Create("lat", "lon")));
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("{\"lat\":1.0,\"lon\":2.0}"), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateStructMissingRequiredField()
        {
            var schema = TR.TypeSchema.Of(new TR.StructTypeRef("Coordinates",
                                                               ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                                     new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                               ImmutableArray.Create("lat", "lon")));
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("{\"lat\":1.0}"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("required field 'lon' missing")));
        }

        [TestMethod]
        public void ValidateStructAdditionalProperty()
        {
            var schema = TR.TypeSchema.Of(new TR.StructTypeRef("Coordinates",
                                                               ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                                     new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                               ImmutableArray.Create("lat", "lon")));
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("{\"lat\":1.0,\"lon\":2.0,\"extra\":3.0}"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("unknown field 'extra'")));
        }

        [TestMethod]
        public void ValidateStructFieldRangeViolation()
        {
            var fieldAnnotations = ImmutableDictionary<string, TR.TypeAnnotations>.Empty.Add("lat", new TR.TypeAnnotations { Minimum = -90, Maximum = 90 });
            var schema = new TR.TypeSchema(new TR.StructTypeRef("Coordinates",
                                                                ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                                      new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                                ImmutableArray.Create("lat", "lon")),
                                           TR.TypeAnnotations.None,
                                           fieldAnnotations);
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("{\"lat\":200.0,\"lon\":8.5}"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("$.lat")));
        }

        [TestMethod]
        public void ValidateNullableNullValid()
        {
            var schema = TR.TypeSchema.Of(new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)));
            var result = PropertyValueCodec.ValidateJson(null, schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateNullForNonNullableInvalid()
        {
            var schema = TR.TypeSchema.Of(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var result = PropertyValueCodec.ValidateJson(null, schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("null")));
        }

        [TestMethod]
        public void ValidateArrayShape()
        {
            var schema = TR.TypeSchema.Of(new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)));
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("[1.0,2.0,3.0]"), schema);
            Assert.IsTrue(result.IsValid);
            Assert.IsEmpty(result.Errors);
        }

        [TestMethod]
        public void ValidateArrayElementWrongType()
        {
            var schema = TR.TypeSchema.Of(new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)));
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("[1.0,\"string\",3.0]"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("[1]")));
        }

        [TestMethod]
        public void ValidateArrayOfStructsWithFieldError()
        {
            var fieldAnnotations = ImmutableDictionary<string, TR.TypeAnnotations>.Empty.Add("lat", new TR.TypeAnnotations { Minimum = -90, Maximum = 90 });
            var structSchema = new TR.StructTypeRef("Coordinates",
                                                    ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                          new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                    ImmutableArray.Create("lat", "lon"));
            var schema = new TR.TypeSchema(new TR.ArrayTypeRef(structSchema), TR.TypeAnnotations.None, fieldAnnotations);
            var result = PropertyValueCodec.ValidateJson(JsonNode.Parse("[{\"lat\":1.0,\"lon\":2.0},{\"lat\":200.0,\"lon\":8.5}]"), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("[1].lat")));
        }

        [TestMethod]
        public void ValidateReadOnlyRejectsAllValues()
        {
            var schema = new TR.TypeSchema(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double),
                                           new TR.TypeAnnotations { ReadOnly = true },
                                           ImmutableDictionary<string, TR.TypeAnnotations>.Empty);
            var result = PropertyValueCodec.ValidateJson(JsonValue.Create(1.0), schema);
            Assert.IsFalse(result.IsValid);
            Assert.IsTrue(result.Errors.Any(e => e.Contains("read-only")));
        }
    }
}
