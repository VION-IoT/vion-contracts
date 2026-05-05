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
            var bytes = BuildBoolArrayVal();
            Assert.Throws<PropertyValueDecodeException>(() => PropertyValueCodec.FlatBufferToJson(bytes));
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

        private static byte[] BuildBoolArrayVal()
        {
            var builder = new FlatBufferBuilder(64);
            var inner = BoolArray.CreateBoolArray(builder);
            var pv = PropertyValue.CreatePropertyValue(builder, ValuePayload.BoolArray, inner.Value);
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
