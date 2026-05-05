using System;
using System.Globalization;
using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml;
using Google.FlatBuffers;
using Vion.Contracts.Codec;
using Vion.Contracts.FlatBuffers.Common;
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
        [TestMethod]
        public void DecodeBoolValTrueAsJsonBool()
        {
            var bytes = BuildBoolVal(true);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.IsTrue(json!.GetValue<bool>());
        }

        [TestMethod]
        public void DecodeBoolValFalseAsJsonBool()
        {
            var bytes = BuildBoolVal(false);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.IsFalse(json!.GetValue<bool>());
        }

        [TestMethod]
        public void DecodeLongValAsJsonLong()
        {
            var bytes = BuildLongVal(9876543210L);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual(9876543210L, json!.GetValue<long>());
        }

        [TestMethod]
        public void DecodeDoubleValAsJsonDouble()
        {
            var bytes = BuildDoubleVal(3.14159);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual(3.14159, json!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeStringValAsJsonString()
        {
            var bytes = BuildStringVal("hello world");
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual("hello world", json!.GetValue<string>());
        }

        [TestMethod]
        public void DecodeDateTimeValAsRfc3339String()
        {
            const long unixMs = 1717372800000L;
            var bytes = BuildDateTimeVal(unixMs);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var expected = DateTimeOffset.FromUnixTimeMilliseconds(unixMs).UtcDateTime.ToString("o");
            Assert.AreEqual(expected, json!.GetValue<string>());
        }

        [TestMethod]
        public void DecodeDurationValAsIso8601String()
        {
            // 90 minutes = 54_000_000_000 ticks
            const long ticks = 54_000_000_000L;
            var bytes = BuildDurationVal(ticks);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var expected = XmlConvert.ToString(TimeSpan.FromTicks(ticks));
            Assert.AreEqual(expected, json!.GetValue<string>());
        }

        [TestMethod]
        public void DecodeNonePayloadAsNull()
        {
            var bytes = BuildNoneVal();
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNull(json);
        }

        [TestMethod]
        public void ThrowsOnUnknownPayloadType()
        {
            // Forge a PropertyValue with an out-of-range payload tag; exercises the codec's default-throw branch.
            var bytes = BuildPropertyValueWithRawTag((ValuePayload)99);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.FlatBufferToJson(bytes));
        }

        [TestMethod]
        public void DecodeStructValWithSinglePrimitiveField()
        {
            var bytes = BuildStructValAllDoubles(("lat", 47.3));
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.AreEqual(1, obj.Count);
            Assert.AreEqual(47.3, obj["lat"]!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeStructValWithMultipleFields()
        {
            var bytes = BuildStructValAllDoubles(("lat", 47.3), ("lon", 8.5), ("altitude", 423.0));
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.AreEqual(3, obj.Count);
            Assert.AreEqual(47.3, obj["lat"]!.GetValue<double>());
            Assert.AreEqual(8.5, obj["lon"]!.GetValue<double>());
            Assert.AreEqual(423.0, obj["altitude"]!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeStructValPreservesFieldOrder()
        {
            var bytes = BuildStructValAllDoubles(("a", 1.0), ("b", 2.0), ("c", 3.0));
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var keys = json!.AsObject().Select(kvp => kvp.Key).ToArray();
            Assert.AreEqual("a", keys[0]);
            Assert.AreEqual("b", keys[1]);
            Assert.AreEqual("c", keys[2]);
        }

        [TestMethod]
        public void DecodeStructValWithMixedFieldTypes()
        {
            var bytes = BuildStructValWithCallbacks(("score", b => (ValuePayload.DoubleVal, DoubleVal.CreateDoubleVal(b, 9.5).Value)),
                                                    ("label", b =>
                                                              {
                                                                  var s = b.CreateString("ok");
                                                                  return (ValuePayload.StringVal, StringVal.CreateStringVal(b, s).Value);
                                                              }),
                                                    ("active", b => (ValuePayload.BoolVal, BoolVal.CreateBoolVal(b, true).Value)));
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.AreEqual(3, obj.Count);
            Assert.AreEqual(9.5, obj["score"]!.GetValue<double>());
            Assert.AreEqual("ok", obj["label"]!.GetValue<string>());
            Assert.IsTrue(obj["active"]!.GetValue<bool>());
        }

        [TestMethod]
        public void DecodeEmptyStructValAsEmptyJsonObject()
        {
            var bytes = BuildStructValBytes();
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual(0, json!.AsObject().Count);
        }

        [TestMethod]
        public void ThrowsOnStructValFieldWithNullValue()
        {
            var bytes = BuildStructValWithNullFieldValue("foo");
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.FlatBufferToJson(bytes));
        }

        [TestMethod]
        public void DecodeStructArrayAsJsonArrayOfObjects()
        {
            var bytes = BuildStructArrayBytes(new[] { new[] { ("lat", 47.3), ("lon", 8.5) }, new[] { ("lat", 48.1), ("lon", 9.2) } });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(47.3, arr[0]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(8.5, arr[0]!.AsObject()["lon"]!.GetValue<double>());
            Assert.AreEqual(48.1, arr[1]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(9.2, arr[1]!.AsObject()["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeStructArrayHonoursPresentMarkers()
        {
            // items=[coords0, coords1, coords2], present=[true, false, true] → [obj, null, obj]
            var bytes = BuildStructArrayBytesWithPresent(new[] { new[] { ("lat", 1.0), ("lon", 2.0) }, new[] { ("lat", 3.0), ("lon", 4.0) }, new[] { ("lat", 5.0), ("lon", 6.0) } },
                                                         new[] { true, false, true });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.IsNotNull(arr[0]);
            Assert.IsNull(arr[1]);
            Assert.IsNotNull(arr[2]);
            Assert.AreEqual(1.0, arr[0]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(5.0, arr[2]!.AsObject()["lat"]!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeEmptyStructArrayAsEmptyJsonArray()
        {
            var bytes = BuildStructArrayBytes(Array.Empty<(string, double)[]>());
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual(0, json!.AsArray().Count);
        }

        [TestMethod]
        public void ThrowsOnStructArrayWithMismatchedPresentLength()
        {
            // items=[a, b], present=[true] → mismatched lengths → PropertyValueDecodeException
            var bytes = BuildStructArrayBytesWithPresent(new[] { new[] { ("x", 1.0) }, new[] { ("x", 2.0) } }, new[] { true });
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.FlatBufferToJson(bytes));
        }

        [TestMethod]
        public void DecodeBoolArrayWithoutPresentAsJsonArray()
        {
            var bytes = BuildBoolArrayVal(new[] { true, false, true });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.IsTrue(arr[0]!.GetValue<bool>());
            Assert.IsFalse(arr[1]!.GetValue<bool>());
            Assert.IsTrue(arr[2]!.GetValue<bool>());
        }

        [TestMethod]
        public void DecodeLongArrayAsJsonArray()
        {
            var bytes = BuildLongArrayVal(new[] { 1L, 9876543210L, -42L });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1L, arr[0]!.GetValue<long>());
            Assert.AreEqual(9876543210L, arr[1]!.GetValue<long>());
            Assert.AreEqual(-42L, arr[2]!.GetValue<long>());
        }

        [TestMethod]
        public void DecodeDoubleArrayAsJsonArray()
        {
            var bytes = BuildDoubleArrayVal(new[] { 1.1, 2.2, 3.3 });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1.1, arr[0]!.GetValue<double>());
            Assert.AreEqual(2.2, arr[1]!.GetValue<double>());
            Assert.AreEqual(3.3, arr[2]!.GetValue<double>());
        }

        [TestMethod]
        public void DecodeStringArrayAsJsonArray()
        {
            var bytes = BuildStringArrayVal(new[] { "alpha", "beta", "gamma" });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual("alpha", arr[0]!.GetValue<string>());
            Assert.AreEqual("beta", arr[1]!.GetValue<string>());
            Assert.AreEqual("gamma", arr[2]!.GetValue<string>());
        }

        [TestMethod]
        public void DecodeDateTimeArrayAsJsonArray()
        {
            var unixMsValues = new[] { 0L, 1717372800000L, 1000000000000L };
            var bytes = BuildDateTimeArrayVal(unixMsValues);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            for (var i = 0; i < unixMsValues.Length; i++)
            {
                var expected = DateTimeOffset.FromUnixTimeMilliseconds(unixMsValues[i]).UtcDateTime.ToString("o");
                Assert.AreEqual(expected, arr[i]!.GetValue<string>());
            }
        }

        [TestMethod]
        public void DecodeDurationArrayAsJsonArray()
        {
            var ticksValues = new[] { 0L, 54_000_000_000L, 36_000_000_000L };
            var bytes = BuildDurationArrayVal(ticksValues);
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            for (var i = 0; i < ticksValues.Length; i++)
            {
                var expected = XmlConvert.ToString(TimeSpan.FromTicks(ticksValues[i]));
                Assert.AreEqual(expected, arr[i]!.GetValue<string>());
            }
        }

        [TestMethod]
        public void DecodeArrayWithPresentMarksNullElements()
        {
            // values=[true, false, false], present=[true, false, true] → [true, null, false]
            var bytes = BuildBoolArrayValWithPresent(new[] { true, false, false }, new[] { true, false, true });
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.IsTrue(arr[0]!.GetValue<bool>());
            Assert.IsNull(arr[1]);
            Assert.IsFalse(arr[2]!.GetValue<bool>());
        }

        [TestMethod]
        public void ThrowsOnArrayWithMismatchedPresentLength()
        {
            // values=[true, false], present=[true] → mismatched lengths → PropertyValueDecodeException
            var bytes = BuildBoolArrayValWithPresent(new[] { true, false }, new[] { true });
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.FlatBufferToJson(bytes));
        }

        [TestMethod]
        public void DecodeEmptyArrayAsEmptyJsonArray()
        {
            var bytes = BuildBoolArrayVal(Array.Empty<bool>());
            var json = PropertyValueCodec.FlatBufferToJson(bytes);
            Assert.IsNotNull(json);
            Assert.AreEqual(0, json!.AsArray().Count);
        }

        // ── CLR-encode round-trip tests (ClrToFlatBuffer → FlatBufferToJson) ──

        [TestMethod]
        public void ClrEncodeBoolWritesJsonBool()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Bool);
            var json = ClrEncodeAndDecode(true, schema);
            Assert.IsNotNull(json);
            Assert.IsTrue(json!.GetValue<bool>());
        }

        [TestMethod]
        public void ClrEncodeStringWritesJsonString()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.String);
            var json = ClrEncodeAndDecode("hello clr", schema);
            Assert.IsNotNull(json);
            Assert.AreEqual("hello clr", json!.GetValue<string>());
        }

        [TestMethod]
        public void ClrEncodeLongWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long);
            var json = ClrEncodeAndDecode(42L, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(42L, json!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeIntWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Int);
            var json = ClrEncodeAndDecode((int)42, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(42L, json!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeUIntWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.UInt);
            var json = ClrEncodeAndDecode(4_000_000_000u, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(4_000_000_000L, json!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeByteWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Byte);
            var json = ClrEncodeAndDecode((byte)200, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(200L, json!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeDoubleWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double);
            var json = ClrEncodeAndDecode(3.14, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(3.14, json!.GetValue<double>(), 1e-9);
        }

        [TestMethod]
        public void ClrEncodeFloatWritesJsonNumber()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Float);
            var json = ClrEncodeAndDecode((float)1.5f, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(1.5, json!.GetValue<double>(), 0.001);
        }

        [TestMethod]
        public void ClrEncodeDateTimeWritesJsonString()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime);
            var dt = new DateTime(2024,
                                  6,
                                  3,
                                  12,
                                  0,
                                  0,
                                  DateTimeKind.Utc);
            var json = ClrEncodeAndDecode(dt, schema);
            Assert.IsNotNull(json);
            var parsed = DateTimeOffset.Parse(json!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            Assert.AreEqual(dt, parsed.UtcDateTime);
        }

        [TestMethod]
        public void ClrEncodeTimeSpanWritesJsonString()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Duration);
            var json = ClrEncodeAndDecode(TimeSpan.FromMinutes(90), schema);
            Assert.IsNotNull(json);
            var parsed = XmlConvert.ToTimeSpan(json!.GetValue<string>());
            Assert.AreEqual(TimeSpan.FromMinutes(90), parsed);
        }

        [TestMethod]
        public void ClrEncodeLocalDateTimeThrows()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime);
            var localDt = DateTime.SpecifyKind(new DateTime(2024,
                                                            1,
                                                            1,
                                                            0,
                                                            0,
                                                            0),
                                               DateTimeKind.Local);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToFlatBuffer(localDt, schema));
        }

        [TestMethod]
        public void ClrEncodeEnumWritesMemberName()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var json = ClrEncodeAndDecode(AlarmState.Warning, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual("Warning", json!.GetValue<string>());
        }

        [TestMethod]
        public void ClrEncodeUndefinedEnumValueThrows()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToFlatBuffer((AlarmState)99, schema));
        }

        [TestMethod]
        public void ClrEncodeEnumExercisesCache()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var json1 = ClrEncodeAndDecode(AlarmState.Ok, schema);
            var json2 = ClrEncodeAndDecode(AlarmState.Critical, schema);
            Assert.IsNotNull(json1);
            Assert.IsNotNull(json2);
            Assert.AreEqual("Ok", json1!.GetValue<string>());
            Assert.AreEqual("Critical", json2!.GetValue<string>());
        }

        [TestMethod]
        public void ClrEncodeStructWritesJsonObjectWithCamelCaseKeys()
        {
            var schema = new TR.StructTypeRef("Coordinates",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon"));
            var json = ClrEncodeAndDecode(new Coordinates(47.3, 8.5), schema);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.IsTrue(obj.ContainsKey("lat"));
            Assert.IsTrue(obj.ContainsKey("lon"));
            Assert.AreEqual(47.3, obj["lat"]!.GetValue<double>());
            Assert.AreEqual(8.5, obj["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void ClrEncodeStructPreservesSchemaFieldOrder()
        {
            var schema = new TR.StructTypeRef("Coordinates3D",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("altitude", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon", "altitude"));
            var json = ClrEncodeAndDecode(new Coordinates3D(47.3, 8.5, 423.0), schema);
            Assert.IsNotNull(json);
            var keys = json!.AsObject().Select(kvp => kvp.Key).ToArray();
            Assert.AreEqual("lat", keys[0]);
            Assert.AreEqual("lon", keys[1]);
            Assert.AreEqual("altitude", keys[2]);
        }

        [TestMethod]
        public void ClrEncodeStructWithMissingClrPropertyThrows()
        {
            var schema = new TR.StructTypeRef("Coordinates",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("missing", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon", "missing"));
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToFlatBuffer(new Coordinates(1.0, 2.0), schema));
        }

        [TestMethod]
        public void ClrEncodeNullablePrimitiveValueWrites()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var json = ClrEncodeAndDecode((double?)42.0, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(42.0, json!.GetValue<double>());
        }

        [TestMethod]
        public void ClrEncodeNullablePrimitiveNullWritesJsonNull()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var json = ClrEncodeAndDecode((double?)null, schema);
            Assert.IsNull(json);
        }

        [TestMethod]
        public void ClrEncodeNullableStructValue()
        {
            var coordSchema = new TR.StructTypeRef("Coordinates",
                                                   ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                         new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                   ImmutableArray.Create("lat", "lon"));
            var schema = new TR.NullableTypeRef(coordSchema);
            var json = ClrEncodeAndDecode((Coordinates?)new Coordinates(1.0, 2.0), schema);
            Assert.IsNotNull(json);
            var obj = json!.AsObject();
            Assert.AreEqual(1.0, obj["lat"]!.GetValue<double>());
            Assert.AreEqual(2.0, obj["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void ClrEncodeNullableStructNull()
        {
            var coordSchema = new TR.StructTypeRef("Coordinates",
                                                   ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                         new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                   ImmutableArray.Create("lat", "lon"));
            var schema = new TR.NullableTypeRef(coordSchema);
            var json = ClrEncodeAndDecode((Coordinates?)null, schema);
            Assert.IsNull(json);
        }

        [TestMethod]
        public void ClrEncodeNullForNonNullablePrimitiveThrows()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.ClrToFlatBuffer(null, schema));
        }

        [TestMethod]
        public void ClrEncodeImmutableArrayLong()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var json = ClrEncodeAndDecode(ImmutableArray.Create(1L, 2L, 3L), schema);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1L, arr[0]!.GetValue<long>());
            Assert.AreEqual(2L, arr[1]!.GetValue<long>());
            Assert.AreEqual(3L, arr[2]!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeImmutableArrayStruct()
        {
            var coordSchema = new TR.StructTypeRef("Coordinates",
                                                   ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                         new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                   ImmutableArray.Create("lat", "lon"));
            var schema = new TR.ArrayTypeRef(coordSchema);
            var json = ClrEncodeAndDecode(ImmutableArray.Create(new Coordinates(1.0, 2.0), new Coordinates(3.0, 4.0)), schema);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(1.0, arr[0]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(2.0, arr[0]!.AsObject()["lon"]!.GetValue<double>());
            Assert.AreEqual(3.0, arr[1]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(4.0, arr[1]!.AsObject()["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void ClrEncodeImmutableArrayEnum()
        {
            var schema = new TR.ArrayTypeRef(new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var json = ClrEncodeAndDecode(ImmutableArray.Create(AlarmState.Ok, AlarmState.Warning), schema);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual("Ok", arr[0]!.GetValue<string>());
            Assert.AreEqual("Warning", arr[1]!.GetValue<string>());
        }

        [TestMethod]
        public void ClrEncodeImmutableArrayNullableLong()
        {
            var schema = new TR.ArrayTypeRef(new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long)));
            var json = ClrEncodeAndDecode(ImmutableArray.Create<long?>(1L, null, 2L), schema);
            Assert.IsNotNull(json);
            var arr = json!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1L, arr[0]!.GetValue<long>());
            Assert.IsNull(arr[1]);
            Assert.AreEqual(2L, arr[2]!.GetValue<long>());
        }

        [TestMethod]
        public void ClrEncodeEmptyImmutableArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var json = ClrEncodeAndDecode(ImmutableArray<long>.Empty, schema);
            Assert.IsNotNull(json);
            Assert.AreEqual(0, json!.AsArray().Count);
        }

        // ── Round-trip tests (JsonToFlatBuffer → FlatBufferToJson) ────────────

        [TestMethod]
        public void RoundtripBool()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Bool);
            var rt = Roundtrip(JsonValue.Create(true), schema);
            Assert.IsTrue(rt!.GetValue<bool>());
        }

        [TestMethod]
        public void RoundtripString()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.String);
            var rt = Roundtrip(JsonValue.Create("hello"), schema);
            Assert.AreEqual("hello", rt!.GetValue<string>());
        }

        [TestMethod]
        public void RoundtripByte()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Byte);
            var rt = Roundtrip(JsonNode.Parse("200"), schema);
            Assert.AreEqual(200L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripShort()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Short);
            var rt = Roundtrip(JsonNode.Parse("1000"), schema);
            Assert.AreEqual(1000L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripUShort()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.UShort);
            var rt = Roundtrip(JsonNode.Parse("60000"), schema);
            Assert.AreEqual(60000L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripInt()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Int);
            var rt = Roundtrip(JsonNode.Parse("2147483647"), schema);
            Assert.AreEqual(2147483647L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripUInt()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.UInt);
            var rt = Roundtrip(JsonNode.Parse("4294967295"), schema);
            Assert.AreEqual(4294967295L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripLong()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long);
            var rt = Roundtrip(JsonNode.Parse("9876543210"), schema);
            Assert.AreEqual(9876543210L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripFloat()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Float);
            var rt = Roundtrip(JsonNode.Parse("3.14"), schema);
            Assert.AreEqual(3.14, rt!.GetValue<double>(), 0.001);
        }

        [TestMethod]
        public void RoundtripDouble()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double);
            var rt = Roundtrip(JsonNode.Parse("2.718281828"), schema);
            Assert.AreEqual(2.718281828, rt!.GetValue<double>(), 1e-9);
        }

        [TestMethod]
        public void RoundtripDateTime()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime);
            const string input = "2024-06-03T12:34:56Z";
            var rt = Roundtrip(JsonValue.Create(input), schema);
            var parsedRt = DateTimeOffset.Parse(rt!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            var parsedInput = DateTimeOffset.Parse(input, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            Assert.AreEqual(parsedInput.UtcDateTime, parsedRt.UtcDateTime);
        }

        [TestMethod]
        public void RoundtripDuration()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Duration);
            const string input = "PT1H30M";
            var rt = Roundtrip(JsonValue.Create(input), schema);
            var parsedRt = XmlConvert.ToTimeSpan(rt!.GetValue<string>());
            var parsedInput = XmlConvert.ToTimeSpan(input);
            Assert.AreEqual(parsedInput, parsedRt);
        }

        [TestMethod]
        public void RoundtripNullablePrimitive_NullValue()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var rt = Roundtrip(null, schema);
            Assert.IsNull(rt);
        }

        [TestMethod]
        public void RoundtripNullablePrimitive_NonNullValue()
        {
            var schema = new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var rt = Roundtrip(JsonNode.Parse("42"), schema);
            Assert.AreEqual(42L, rt!.GetValue<long>());
        }

        [TestMethod]
        public void ThrowsOnNullForNonNullableType()
        {
            var schema = new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long);
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToFlatBuffer(null, schema));
        }

        [TestMethod]
        public void RoundtripEnumMemberName()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var rt = Roundtrip(JsonValue.Create("Warning"), schema);
            Assert.AreEqual("Warning", rt!.GetValue<string>());
        }

        [TestMethod]
        public void EncoderDoesNotValidateEnumMembership()
        {
            var schema = new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical"));
            var rt = Roundtrip(JsonValue.Create("NotAMember"), schema);
            Assert.AreEqual("NotAMember", rt!.GetValue<string>());
        }

        // ── Struct round-trip tests ───────────────────────────────────────────

        [TestMethod]
        public void RoundtripStructWithFlatFields()
        {
            var schema = new TR.StructTypeRef("Coordinates",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon"));
            var input = JsonNode.Parse("{\"lat\":47.3,\"lon\":8.5}");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var obj = rt!.AsObject();
            Assert.AreEqual(47.3, obj["lat"]!.GetValue<double>());
            Assert.AreEqual(8.5, obj["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void RoundtripStructPreservesFieldOrder()
        {
            var schema = new TR.StructTypeRef("Coordinates3D",
                                              ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("altitude", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("lat", "lon", "altitude"));

            // Input JSON keys in non-schema order
            var input = JsonNode.Parse("{\"lon\":8.5,\"altitude\":423.0,\"lat\":47.3}");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var keys = rt!.AsObject().Select(kvp => kvp.Key).ToArray();
            Assert.AreEqual("lat", keys[0]);
            Assert.AreEqual("lon", keys[1]);
            Assert.AreEqual("altitude", keys[2]);
        }

        [TestMethod]
        public void RoundtripStructWithNullableField()
        {
            var schema = new TR.StructTypeRef("X",
                                              ImmutableArray.Create(new TR.StructField("a", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("b", new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)))),
                                              ImmutableArray.Create("a", "b"));
            var input = JsonNode.Parse("{\"a\":1.0,\"b\":null}");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var obj = rt!.AsObject();
            Assert.AreEqual(1.0, obj["a"]!.GetValue<double>());
            Assert.IsNull(obj["b"]);
        }

        [TestMethod]
        public void ThrowsOnStructMissingRequiredField()
        {
            var schema = new TR.StructTypeRef("X",
                                              ImmutableArray.Create(new TR.StructField("a", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                    new TR.StructField("b", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                              ImmutableArray.Create("a", "b"));
            var input = JsonNode.Parse("{\"a\":1.0}");
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToFlatBuffer(input, schema));
        }

        [TestMethod]
        public void RoundtripEmptyStructIsEmptyObject()
        {
            var schema = new TR.StructTypeRef("Empty", ImmutableArray<TR.StructField>.Empty, ImmutableArray<string>.Empty);
            var input = JsonNode.Parse("{}");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            Assert.AreEqual(0, rt!.AsObject().Count);
        }

        // ── Array round-trip tests ────────────────────────────────────────────

        [TestMethod]
        public void RoundtripBoolArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Bool));
            var input = JsonNode.Parse("[true,false,true]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.IsTrue(arr[0]!.GetValue<bool>());
            Assert.IsFalse(arr[1]!.GetValue<bool>());
            Assert.IsTrue(arr[2]!.GetValue<bool>());
        }

        [TestMethod]
        public void RoundtripLongArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var input = JsonNode.Parse("[1,2,9876543210]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1L, arr[0]!.GetValue<long>());
            Assert.AreEqual(2L, arr[1]!.GetValue<long>());
            Assert.AreEqual(9876543210L, arr[2]!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripDoubleArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double));
            var input = JsonNode.Parse("[1.1,2.2,3.3]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1.1, arr[0]!.GetValue<double>(), 1e-9);
            Assert.AreEqual(2.2, arr[1]!.GetValue<double>(), 1e-9);
            Assert.AreEqual(3.3, arr[2]!.GetValue<double>(), 1e-9);
        }

        [TestMethod]
        public void RoundtripStringArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.String));
            var input = JsonNode.Parse("[\"a\",\"b\",\"c\"]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual("a", arr[0]!.GetValue<string>());
            Assert.AreEqual("b", arr[1]!.GetValue<string>());
            Assert.AreEqual("c", arr[2]!.GetValue<string>());
        }

        [TestMethod]
        public void RoundtripDateTimeArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.DateTime));
            var inputs = new[] { "2024-01-01T00:00:00Z", "2024-06-03T12:34:56Z", "2000-01-01T00:00:00Z" };
            var input = JsonNode.Parse($"[\"{inputs[0]}\",\"{inputs[1]}\",\"{inputs[2]}\"]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            for (var i = 0; i < inputs.Length; i++)
            {
                var expected = DateTimeOffset.Parse(inputs[i], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;
                var actual = DateTimeOffset.Parse(arr[i]!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).UtcDateTime;
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void RoundtripDurationArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Duration));
            var inputs = new[] { "PT0S", "PT1H30M", "P1DT2H" };
            var input = JsonNode.Parse($"[\"{inputs[0]}\",\"{inputs[1]}\",\"{inputs[2]}\"]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            for (var i = 0; i < inputs.Length; i++)
            {
                var expected = XmlConvert.ToTimeSpan(inputs[i]);
                var actual = XmlConvert.ToTimeSpan(arr[i]!.GetValue<string>());
                Assert.AreEqual(expected, actual);
            }
        }

        [TestMethod]
        public void RoundtripArrayOfNullablePrimitives()
        {
            var schema = new TR.ArrayTypeRef(new TR.NullableTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long)));
            var input = JsonNode.Parse("[1,null,2]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.AreEqual(1L, arr[0]!.GetValue<long>());
            Assert.IsNull(arr[1]);
            Assert.AreEqual(2L, arr[2]!.GetValue<long>());
        }

        [TestMethod]
        public void RoundtripStructArray()
        {
            var coordSchema = new TR.StructTypeRef("Coordinates",
                                                   ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                         new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                   ImmutableArray.Create("lat", "lon"));
            var schema = new TR.ArrayTypeRef(coordSchema);
            var input = JsonNode.Parse("[{\"lat\":1.0,\"lon\":2.0},{\"lat\":3.0,\"lon\":4.0}]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual(1.0, arr[0]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(2.0, arr[0]!.AsObject()["lon"]!.GetValue<double>());
            Assert.AreEqual(3.0, arr[1]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(4.0, arr[1]!.AsObject()["lon"]!.GetValue<double>());
        }

        [TestMethod]
        public void RoundtripArrayOfNullableStructs()
        {
            var coordSchema = new TR.StructTypeRef("Coordinates",
                                                   ImmutableArray.Create(new TR.StructField("lat", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double)),
                                                                         new TR.StructField("lon", new TR.PrimitiveTypeRef(TR.PrimitiveKind.Double))),
                                                   ImmutableArray.Create("lat", "lon"));
            var schema = new TR.ArrayTypeRef(new TR.NullableTypeRef(coordSchema));
            var input = JsonNode.Parse("[{\"lat\":1.0,\"lon\":2.0},null,{\"lat\":3.0,\"lon\":4.0}]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(3, arr.Count);
            Assert.IsNotNull(arr[0]);
            Assert.IsNull(arr[1]);
            Assert.IsNotNull(arr[2]);
            Assert.AreEqual(1.0, arr[0]!.AsObject()["lat"]!.GetValue<double>());
            Assert.AreEqual(3.0, arr[2]!.AsObject()["lat"]!.GetValue<double>());
        }

        [TestMethod]
        public void RoundtripEnumArray()
        {
            var schema = new TR.ArrayTypeRef(new TR.EnumTypeRef("AlarmState", ImmutableArray.Create("Ok", "Warning", "Critical")));
            var input = JsonNode.Parse("[\"Ok\",\"Warning\"]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            var arr = rt!.AsArray();
            Assert.AreEqual(2, arr.Count);
            Assert.AreEqual("Ok", arr[0]!.GetValue<string>());
            Assert.AreEqual("Warning", arr[1]!.GetValue<string>());
        }

        [TestMethod]
        public void RoundtripEmptyArrayIsEmpty()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var input = JsonNode.Parse("[]");
            var rt = Roundtrip(input, schema);
            Assert.IsNotNull(rt);
            Assert.AreEqual(0, rt!.AsArray().Count);
        }

        // ── Negative tests ────────────────────────────────────────────────────

        [TestMethod]
        public void ThrowsOnNestedArrayType()
        {
            var schema = new TR.ArrayTypeRef(new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long)));
            var input = JsonNode.Parse("[[1,2]]");
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToFlatBuffer(input, schema));
        }

        [TestMethod]
        public void ThrowsOnNullElementForNonNullableItemType()
        {
            var schema = new TR.ArrayTypeRef(new TR.PrimitiveTypeRef(TR.PrimitiveKind.Long));
            var input = JsonNode.Parse("[1,null,2]");
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.JsonToFlatBuffer(input, schema));
        }

        private static byte[] BuildBoolVal(bool v)
        {
            var builder = new FlatBufferBuilder(64);
            var inner = BoolVal.CreateBoolVal(builder, v);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.BoolVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildLongVal(long v)
        {
            var builder = new FlatBufferBuilder(64);
            var inner = LongVal.CreateLongVal(builder, v);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.LongVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDoubleVal(double v)
        {
            var builder = new FlatBufferBuilder(64);
            var inner = DoubleVal.CreateDoubleVal(builder, v);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DoubleVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildStringVal(string v)
        {
            var builder = new FlatBufferBuilder(128);
            var strOffset = builder.CreateString(v);
            var inner = StringVal.CreateStringVal(builder, strOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StringVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDateTimeVal(long unixMs)
        {
            var builder = new FlatBufferBuilder(64);
            var inner = DateTimeVal.CreateDateTimeVal(builder, unixMs);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DateTimeVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDurationVal(long ticks)
        {
            var builder = new FlatBufferBuilder(64);
            var inner = DurationVal.CreateDurationVal(builder, ticks);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DurationVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildBoolArrayVal(bool[] values)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = BoolArray.CreateValuesVector(builder, values);
            var inner = BoolArray.CreateBoolArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.BoolArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildBoolArrayValWithPresent(bool[] values, bool[] present)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = BoolArray.CreateValuesVector(builder, values);
            var presentOffset = BoolArray.CreatePresentVector(builder, present);
            var inner = BoolArray.CreateBoolArray(builder, valuesOffset, presentOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.BoolArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildLongArrayVal(long[] values)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = LongArray.CreateValuesVector(builder, values);
            var inner = LongArray.CreateLongArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.LongArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDoubleArrayVal(double[] values)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = DoubleArray.CreateValuesVector(builder, values);
            var inner = DoubleArray.CreateDoubleArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DoubleArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildStringArrayVal(string[] values)
        {
            var builder = new FlatBufferBuilder(256);
            var strOffsets = new StringOffset[values.Length];
            for (var i = 0; i < values.Length; i++)
            {
                strOffsets[i] = builder.CreateString(values[i]);
            }

            var valuesOffset = StringArray.CreateValuesVector(builder, strOffsets);
            var inner = StringArray.CreateStringArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StringArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDateTimeArrayVal(long[] unixMsValues)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = DateTimeArray.CreateUnixMsVector(builder, unixMsValues);
            var inner = DateTimeArray.CreateDateTimeArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DateTimeArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildDurationArrayVal(long[] ticksValues)
        {
            var builder = new FlatBufferBuilder(128);
            var valuesOffset = DurationArray.CreateTicksVector(builder, ticksValues);
            var inner = DurationArray.CreateDurationArray(builder, valuesOffset);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DurationArray, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static byte[] BuildNoneVal()
        {
            var builder = new FlatBufferBuilder(64);
            var pv = PropertyValue.CreatePropertyValue(builder);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // --- Struct helpers ---

        private static byte[] BuildPropertyValueWithRawTag(ValuePayload payloadType)
        {
            var builder = new FlatBufferBuilder(64);
            var pv = PropertyValue.CreatePropertyValue(builder, payloadType);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructVal PropertyValue with zero fields (empty struct).
        private static byte[] BuildStructValBytes()
        {
            var builder = new FlatBufferBuilder(64);
            var inner = StructVal.CreateStructVal(builder);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructVal, inner.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructVal with all-double fields. Fields are name+value pairs.
        private static byte[] BuildStructValAllDoubles(params (string Name, double Value)[] fields)
        {
            var builder = new FlatBufferBuilder(256);
            var fieldOffsets = new Offset<NamedValue>[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var doubleInner = DoubleVal.CreateDoubleVal(builder, fields[i].Value);
                var innerPv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DoubleVal, doubleInner.Value);
                var nameOffset = builder.CreateString(fields[i].Name);
                fieldOffsets[i] = NamedValue.CreateNamedValue(builder, nameOffset, innerPv);
            }

            var fieldsVec = StructVal.CreateFieldsVector(builder, fieldOffsets);
            var sv = StructVal.CreateStructVal(builder, fieldsVec);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructVal, sv.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructVal with heterogeneous fields via per-field builder callbacks.
        // Each callback receives the shared builder and returns (payloadType, payloadOffset).
        // Note: FlatBuffers requires all nested objects (strings, tables) to be built before the
        // containing table. Callbacks must create their inner objects before returning. All objects
        // share one builder so offsets remain valid.
        private static byte[] BuildStructValWithCallbacks(params (string Name, Func<FlatBufferBuilder, (ValuePayload, int)> ValueBuilder)[] fields)
        {
            var builder = new FlatBufferBuilder(512);
            var fieldOffsets = new Offset<NamedValue>[fields.Length];
            for (var i = 0; i < fields.Length; i++)
            {
                var (payloadType, payloadOffset) = fields[i].ValueBuilder(builder);
                var innerPv = PropertyValue.CreatePropertyValue(builder, payloadType, payloadOffset);
                var nameOffset = builder.CreateString(fields[i].Name);
                fieldOffsets[i] = NamedValue.CreateNamedValue(builder, nameOffset, innerPv);
            }

            var fieldsVec = StructVal.CreateFieldsVector(builder, fieldOffsets);
            var sv = StructVal.CreateStructVal(builder, fieldsVec);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructVal, sv.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructVal with a field whose Value offset is 0 (null), to exercise the null-value guard.
        private static byte[] BuildStructValWithNullFieldValue(string fieldName)
        {
            var builder = new FlatBufferBuilder(128);
            var nameOffset = builder.CreateString(fieldName);

            // Pass default Offset<PropertyValue> (zero offset = null value reference).
            var fieldOffset = NamedValue.CreateNamedValue(builder, nameOffset);
            var fieldsVec = StructVal.CreateFieldsVector(builder, new[] { fieldOffset });
            var sv = StructVal.CreateStructVal(builder, fieldsVec);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructVal, sv.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructArray from an array of coordinate-like double-field items, no present vector.
        private static byte[] BuildStructArrayBytes((string Name, double Value)[][] items)
        {
            var builder = new FlatBufferBuilder(512);
            var itemOffsets = new Offset<StructVal>[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var fieldOffsets = new Offset<NamedValue>[items[i].Length];
                for (var j = 0; j < items[i].Length; j++)
                {
                    var doubleInner = DoubleVal.CreateDoubleVal(builder, items[i][j].Value);
                    var innerPv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DoubleVal, doubleInner.Value);
                    var nameOffset = builder.CreateString(items[i][j].Name);
                    fieldOffsets[j] = NamedValue.CreateNamedValue(builder, nameOffset, innerPv);
                }

                var fieldsVec = StructVal.CreateFieldsVector(builder, fieldOffsets);
                itemOffsets[i] = StructVal.CreateStructVal(builder, fieldsVec);
            }

            var itemsVec = StructArray.CreateItemsVector(builder, itemOffsets);
            var sa = StructArray.CreateStructArray(builder, itemsVec);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructArray, sa.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        // Builds a StructArray with a present vector.
        private static byte[] BuildStructArrayBytesWithPresent((string Name, double Value)[][] items, bool[] present)
        {
            var builder = new FlatBufferBuilder(512);
            var itemOffsets = new Offset<StructVal>[items.Length];
            for (var i = 0; i < items.Length; i++)
            {
                var fieldOffsets = new Offset<NamedValue>[items[i].Length];
                for (var j = 0; j < items[i].Length; j++)
                {
                    var doubleInner = DoubleVal.CreateDoubleVal(builder, items[i][j].Value);
                    var innerPv = PropertyValue.CreatePropertyValue(builder, ValuePayload.DoubleVal, doubleInner.Value);
                    var nameOffset = builder.CreateString(items[i][j].Name);
                    fieldOffsets[j] = NamedValue.CreateNamedValue(builder, nameOffset, innerPv);
                }

                var fieldsVec = StructVal.CreateFieldsVector(builder, fieldOffsets);
                itemOffsets[i] = StructVal.CreateStructVal(builder, fieldsVec);
            }

            var itemsVec = StructArray.CreateItemsVector(builder, itemOffsets);
            var presentVec = StructArray.CreatePresentVector(builder, present);
            var sa = StructArray.CreateStructArray(builder, itemsVec, presentVec);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructArray, sa.Value);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
        }

        private static JsonNode? Roundtrip(JsonNode? input, TR.TypeRef type)
        {
            var bytes = PropertyValueCodec.JsonToFlatBuffer(input, type);
            return PropertyValueCodec.FlatBufferToJson(bytes);
        }

        private static JsonNode? ClrEncodeAndDecode(object? clrValue, TR.TypeRef type)
        {
            var bytes = PropertyValueCodec.ClrToFlatBuffer(clrValue, type);
            return PropertyValueCodec.FlatBufferToJson(bytes);
        }
    }
}
