using System;
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
        public void ThrowsOnUnimplementedVariant()
        {
            var bytes = BuildStructValEmpty();
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

        private static byte[] BuildStructValEmpty()
        {
            var builder = new FlatBufferBuilder(64);
            var inner = StructVal.CreateStructVal(builder);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.StructVal, inner.Value);
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
    }
}
