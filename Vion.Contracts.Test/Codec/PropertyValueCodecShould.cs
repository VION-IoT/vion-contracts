using System;
using System.Linq;
using System.Xml;
using Google.FlatBuffers;
using Vion.Contracts.Codec;
using Vion.Contracts.FlatBuffers.Common;

namespace Vion.Contracts.Test.Codec
{
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
    }
}