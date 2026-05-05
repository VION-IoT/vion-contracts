using System;
using System.Text.Json.Nodes;
using System.Xml;
using Google.FlatBuffers;
using Vion.Contracts.FlatBuffers.Common;

namespace Vion.Contracts.Codec
{
    /// <summary>
    ///     Codec between the FlatBuffers <c>PropertyValue</c> wire format and
    ///     <see cref="JsonNode" /> / CLR values. The FB→JSON direction is schema-free —
    ///     the union tag tree is self-describing.
    /// </summary>
    public static class PropertyValueCodec
    {
        /// <summary>
        ///     Decodes a serialized <c>PropertyValue</c> FlatBuffer into a JSON value.
        ///     Returns <c>null</c> when the union payload is <c>NONE</c> (the wire encoding of a null value).
        /// </summary>
        public static JsonNode? FlatBufferToJson(ReadOnlySpan<byte> bytes)
        {
            var bb = new ByteBuffer(bytes.ToArray());
            var pv = PropertyValue.GetRootAsPropertyValue(bb);
            return DecodePayload(pv);
        }

        private static JsonNode? DecodePayload(PropertyValue pv)
        {
            return pv.PayloadType switch
            {
                ValuePayload.NONE => null,
                ValuePayload.BoolVal => JsonValue.Create(pv.PayloadAsBoolVal().Value),
                ValuePayload.LongVal => JsonValue.Create(pv.PayloadAsLongVal().Value),
                ValuePayload.DoubleVal => JsonValue.Create(pv.PayloadAsDoubleVal().Value),
                ValuePayload.StringVal => JsonValue.Create(pv.PayloadAsStringVal().Value),
                ValuePayload.DateTimeVal => JsonValue.Create(DateTimeOffset.FromUnixTimeMilliseconds(pv.PayloadAsDateTimeVal().UnixMs).UtcDateTime.ToString("o")),
                ValuePayload.DurationVal => JsonValue.Create(XmlConvert.ToString(TimeSpan.FromTicks(pv.PayloadAsDurationVal().Ticks))),
                ValuePayload.BoolArray => DecodeBoolArray(pv.PayloadAsBoolArray()),
                ValuePayload.LongArray => DecodeLongArray(pv.PayloadAsLongArray()),
                ValuePayload.DoubleArray => DecodeDoubleArray(pv.PayloadAsDoubleArray()),
                ValuePayload.StringArray => DecodeStringArray(pv.PayloadAsStringArray()),
                ValuePayload.DateTimeArray => DecodeDateTimeArray(pv.PayloadAsDateTimeArray()),
                ValuePayload.DurationArray => DecodeDurationArray(pv.PayloadAsDurationArray()),
                ValuePayload.StructVal => DecodeStructVal(pv.PayloadAsStructVal()),
                ValuePayload.StructArray => DecodeStructArray(pv.PayloadAsStructArray()),
                _ => throw new PropertyValueDecodeException($"FlatBufferToJson: unknown payload type '{pv.PayloadType}'."),
            };
        }

        private static JsonNode DecodeArray<T>(int valuesLength, int presentLength, Func<int, T> getValue, Func<int, bool> getPresent, Func<T, JsonNode?> elementToJson)
        {
            if (presentLength > 0 && presentLength != valuesLength)
            {
                throw new PropertyValueDecodeException($"FlatBufferToJson: array variant has values[{valuesLength}] but present[{presentLength}].");
            }

            var arr = new JsonArray();
            for (var i = 0; i < valuesLength; i++)
            {
                if (presentLength == 0 || getPresent(i))
                {
                    arr.Add(elementToJson(getValue(i)));
                }
                else
                {
                    arr.Add(null);
                }
            }

            return arr;
        }

        private static JsonNode DecodeBoolArray(BoolArray a)
        {
            return DecodeArray(a.ValuesLength, a.PresentLength, a.Values, a.Present, v => JsonValue.Create(v));
        }

        private static JsonNode DecodeLongArray(LongArray a)
        {
            return DecodeArray(a.ValuesLength, a.PresentLength, a.Values, a.Present, v => JsonValue.Create(v));
        }

        private static JsonNode DecodeDoubleArray(DoubleArray a)
        {
            return DecodeArray(a.ValuesLength, a.PresentLength, a.Values, a.Present, v => JsonValue.Create(v));
        }

        private static JsonNode DecodeStringArray(StringArray a)
        {
            return DecodeArray(a.ValuesLength, a.PresentLength, a.Values, a.Present, v => JsonValue.Create(v));
        }

        private static JsonNode DecodeDateTimeArray(DateTimeArray a)
        {
            return DecodeArray(a.UnixMsLength, a.PresentLength, a.UnixMs, a.Present, v => JsonValue.Create(DateTimeOffset.FromUnixTimeMilliseconds(v).UtcDateTime.ToString("o")));
        }

        private static JsonNode DecodeDurationArray(DurationArray a)
        {
            return DecodeArray(a.TicksLength, a.PresentLength, a.Ticks, a.Present, v => JsonValue.Create(XmlConvert.ToString(TimeSpan.FromTicks(v))));
        }

        private static JsonObject DecodeStructVal(StructVal s)
        {
            var obj = new JsonObject();
            var len = s.FieldsLength;
            for (var i = 0; i < len; i++)
            {
                var field = s.Fields(i) ?? throw new PropertyValueDecodeException($"FlatBufferToJson: StructVal field at index {i} is null.");
                var name = field.Name ?? throw new PropertyValueDecodeException($"FlatBufferToJson: StructVal field at index {i} has null Name.");
                var inner = field.Value ?? throw new PropertyValueDecodeException($"FlatBufferToJson: StructVal field '{name}' has null Value.");
                obj[name] = DecodePayload(inner);
            }

            return obj;
        }

        private static JsonArray DecodeStructArray(StructArray sa)
        {
            var len = sa.ItemsLength;
            var presentLen = sa.PresentLength;
            if (presentLen > 0 && presentLen != len)
            {
                throw new PropertyValueDecodeException($"FlatBufferToJson: StructArray has items[{len}] but present[{presentLen}].");
            }

            var arr = new JsonArray();
            for (var i = 0; i < len; i++)
            {
                if (presentLen > 0 && !sa.Present(i))
                {
                    arr.Add(null);
                    continue;
                }

                var item = sa.Items(i) ?? throw new PropertyValueDecodeException($"FlatBufferToJson: StructArray item at index {i} is null.");
                arr.Add(DecodeStructVal(item));
            }

            return arr;
        }
    }
}