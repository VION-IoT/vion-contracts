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
                _ => throw new PropertyValueDecodeException($"FlatBufferToJson: unhandled or not-yet-implemented variant '{pv.PayloadType}'."),
            };
        }
    }
}