using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Xml;
using Google.FlatBuffers;
using Vion.Contracts.FlatBuffers.Common;
using TR = Vion.Contracts.TypeRef;

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

        // ── Encode ────────────────────────────────────────────────────────────

        /// <summary>
        ///     Encodes a JSON value into a serialized <c>PropertyValue</c> FlatBuffer using
        ///     <paramref name="type" /> to choose the wire variant (e.g. <c>42</c> as Long vs Double,
        ///     <c>"hi"</c> as String vs DateTime vs enum). Null with a <see cref="TR.NullableTypeRef" />
        ///     encodes as <c>NONE</c>.
        /// </summary>
        public static byte[] JsonToFlatBuffer(JsonNode? json, TR.TypeRef type)
        {
            var builder = new FlatBufferBuilder(64);
            var (payloadType, payloadOffset) = EncodeValue(builder, json, type);
            var pv = PropertyValue.CreatePropertyValue(builder, payloadType, payloadOffset);
            builder.Finish(pv.Value);
            return builder.SizedByteArray();
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

        private static (ValuePayload PayloadType, int PayloadOffset) EncodeValue(FlatBufferBuilder b, JsonNode? json, TR.TypeRef type)
        {
            if (type is TR.NullableTypeRef n)
            {
                if (json is null)
                {
                    return (ValuePayload.NONE, 0);
                }

                return EncodeValue(b, json, n.Inner);
            }

            if (json is null)
            {
                throw new PropertyValueDecodeException($"JsonToFlatBuffer: null value is not valid for non-nullable type '{type.GetType().Name}'.");
            }

            return type switch
            {
                TR.PrimitiveTypeRef p => EncodePrimitive(b, json, p),
                TR.EnumTypeRef _ => EncodeEnum(b, json),
                TR.StructTypeRef s => EncodeStruct(b, json, s),
                TR.ArrayTypeRef a => EncodeArray(b, json, a),
                _ => throw new PropertyValueDecodeException($"JsonToFlatBuffer: unknown schema type '{type.GetType().Name}'."),
            };
        }

        private static (ValuePayload, int) EncodePrimitive(FlatBufferBuilder b, JsonNode json, TR.PrimitiveTypeRef p)
        {
            return p.Kind switch
            {
                TR.PrimitiveKind.Bool => (ValuePayload.BoolVal, BoolVal.CreateBoolVal(b, json.GetValue<bool>()).Value),
                TR.PrimitiveKind.String => (ValuePayload.StringVal, StringVal.CreateStringVal(b, b.CreateString(json.GetValue<string>())).Value),

                TR.PrimitiveKind.Byte or TR.PrimitiveKind.Short or TR.PrimitiveKind.UShort or TR.PrimitiveKind.Int or TR.PrimitiveKind.UInt or TR.PrimitiveKind.Long =>
                    (ValuePayload.LongVal, LongVal.CreateLongVal(b, json.GetValue<long>()).Value),

                TR.PrimitiveKind.Float or TR.PrimitiveKind.Double => (ValuePayload.DoubleVal, DoubleVal.CreateDoubleVal(b, json.GetValue<double>()).Value),

                TR.PrimitiveKind.DateTime => (ValuePayload.DateTimeVal,
                                                 DateTimeVal.CreateDateTimeVal(b,
                                                                               DateTimeOffset.Parse(json.GetValue<string>(),
                                                                                                    CultureInfo.InvariantCulture,
                                                                                                    DateTimeStyles.RoundtripKind)
                                                                                             .ToUnixTimeMilliseconds())
                                                            .Value),

                TR.PrimitiveKind.Duration => (ValuePayload.DurationVal, DurationVal.CreateDurationVal(b, XmlConvert.ToTimeSpan(json.GetValue<string>()).Ticks).Value),

                _ => throw new PropertyValueDecodeException($"JsonToFlatBuffer: unknown PrimitiveKind '{p.Kind}'."),
            };
        }

        private static (ValuePayload, int) EncodeEnum(FlatBufferBuilder b, JsonNode json)
        {
            var name = json.GetValue<string>();
            return (ValuePayload.StringVal, StringVal.CreateStringVal(b, b.CreateString(name)).Value);
        }

        private static (ValuePayload, int) EncodeStruct(FlatBufferBuilder b, JsonNode json, TR.StructTypeRef s)
        {
            if (json is not JsonObject obj)
            {
                throw new PropertyValueDecodeException($"JsonToFlatBuffer: expected JSON object for struct '{s.Title}', got '{json.GetType().Name}'.");
            }

            var fieldOffsets = new List<Offset<NamedValue>>(s.Fields.Length);
            foreach (var field in s.Fields)
            {
                if (!obj.TryGetPropertyValue(field.Name, out var fieldJson))
                {
                    if (s.Required.Contains(field.Name))
                    {
                        throw new PropertyValueDecodeException($"JsonToFlatBuffer: required field '{field.Name}' missing on struct '{s.Title}'.");
                    }

                    continue;
                }

                var (innerPayloadType, innerPayloadOffset) = EncodeValue(b, fieldJson, field.Type);
                var innerPv = PropertyValue.CreatePropertyValue(b, innerPayloadType, innerPayloadOffset);
                var nameOffset = b.CreateString(field.Name);
                fieldOffsets.Add(NamedValue.CreateNamedValue(b, nameOffset, innerPv));
            }

            var fieldsVec = StructVal.CreateFieldsVector(b, fieldOffsets.ToArray());
            var sv = StructVal.CreateStructVal(b, fieldsVec);
            return (ValuePayload.StructVal, sv.Value);
        }

        private static (ValuePayload, int) EncodeArray(FlatBufferBuilder b, JsonNode json, TR.ArrayTypeRef arr)
        {
            if (json is not JsonArray ja)
            {
                throw new PropertyValueDecodeException($"JsonToFlatBuffer: expected JSON array, got '{json.GetType().Name}'.");
            }

            var (itemType, allowNullElements) = arr.Items is TR.NullableTypeRef nn ? (nn.Inner, true) : (arr.Items, false);

            if (itemType is TR.ArrayTypeRef)
            {
                throw new PropertyValueDecodeException("JsonToFlatBuffer: nested array types are not allowed by the Dale profile.");
            }

            bool[]? present = null;
            if (allowNullElements)
            {
                present = new bool[ja.Count];
                var anyNull = false;
                for (var i = 0; i < ja.Count; i++)
                {
                    present[i] = ja[i] is not null;
                    if (!present[i])
                    {
                        anyNull = true;
                    }
                }

                if (!anyNull)
                {
                    present = null;
                }
            }
            else
            {
                for (var i = 0; i < ja.Count; i++)
                {
                    if (ja[i] is null)
                    {
                        throw new PropertyValueDecodeException($"JsonToFlatBuffer: array element at index {i} is null but item type is not nullable.");
                    }
                }
            }

            return itemType switch
            {
                TR.PrimitiveTypeRef p => EncodePrimitiveArray(b, ja, p, present),
                TR.EnumTypeRef _ => EncodeStringArray(b, ja, present),
                TR.StructTypeRef s => EncodeStructArray(b, ja, s, present),
                _ => throw new PropertyValueDecodeException($"JsonToFlatBuffer: unhandled array item type '{itemType.GetType().Name}'."),
            };
        }

        private static (ValuePayload, int) EncodePrimitiveArray(FlatBufferBuilder b, JsonArray ja, TR.PrimitiveTypeRef p, bool[]? present)
        {
            return p.Kind switch
            {
                TR.PrimitiveKind.Bool => EncodeBoolArray(b, ja, present),
                TR.PrimitiveKind.Byte or TR.PrimitiveKind.Short or TR.PrimitiveKind.UShort or TR.PrimitiveKind.Int or TR.PrimitiveKind.UInt or TR.PrimitiveKind.Long =>
                    EncodeLongArray(b, ja, present),
                TR.PrimitiveKind.Float or TR.PrimitiveKind.Double => EncodeDoubleArray(b, ja, present),
                TR.PrimitiveKind.String => EncodeStringArray(b, ja, present),
                TR.PrimitiveKind.DateTime => EncodeDateTimeArray(b, ja, present),
                TR.PrimitiveKind.Duration => EncodeDurationArray(b, ja, present),
                _ => throw new PropertyValueDecodeException($"JsonToFlatBuffer: unknown PrimitiveKind '{p.Kind}'."),
            };
        }

        private static (ValuePayload, int) EncodeBoolArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var values = new bool[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                values[i] = present is null || present[i] ? ja[i]!.GetValue<bool>() : false;
            }

            var valuesOff = BoolArray.CreateValuesVector(b, values);
            var presentOff = present is null ? default : BoolArray.CreatePresentVector(b, present);
            var arr = BoolArray.CreateBoolArray(b, valuesOff, presentOff);
            return (ValuePayload.BoolArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeLongArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var values = new long[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                values[i] = present is null || present[i] ? ja[i]!.GetValue<long>() : 0L;
            }

            var valuesOff = LongArray.CreateValuesVector(b, values);
            var presentOff = present is null ? default : LongArray.CreatePresentVector(b, present);
            var arr = LongArray.CreateLongArray(b, valuesOff, presentOff);
            return (ValuePayload.LongArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeDoubleArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var values = new double[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                values[i] = present is null || present[i] ? ja[i]!.GetValue<double>() : 0.0;
            }

            var valuesOff = DoubleArray.CreateValuesVector(b, values);
            var presentOff = present is null ? default : DoubleArray.CreatePresentVector(b, present);
            var arr = DoubleArray.CreateDoubleArray(b, valuesOff, presentOff);
            return (ValuePayload.DoubleArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeStringArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var strOffsets = new StringOffset[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                strOffsets[i] = present is null || present[i] ? b.CreateString(ja[i]!.GetValue<string>()) : b.CreateString("");
            }

            var valuesOff = StringArray.CreateValuesVector(b, strOffsets);
            var presentOff = present is null ? default : StringArray.CreatePresentVector(b, present);
            var arr = StringArray.CreateStringArray(b, valuesOff, presentOff);
            return (ValuePayload.StringArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeDateTimeArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var values = new long[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                values[i] = present is null || present[i] ?
                                DateTimeOffset.Parse(ja[i]!.GetValue<string>(), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind).ToUnixTimeMilliseconds() : 0L;
            }

            var valuesOff = DateTimeArray.CreateUnixMsVector(b, values);
            var presentOff = present is null ? default : DateTimeArray.CreatePresentVector(b, present);
            var arr = DateTimeArray.CreateDateTimeArray(b, valuesOff, presentOff);
            return (ValuePayload.DateTimeArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeDurationArray(FlatBufferBuilder b, JsonArray ja, bool[]? present)
        {
            var values = new long[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                values[i] = present is null || present[i] ? XmlConvert.ToTimeSpan(ja[i]!.GetValue<string>()).Ticks : 0L;
            }

            var valuesOff = DurationArray.CreateTicksVector(b, values);
            var presentOff = present is null ? default : DurationArray.CreatePresentVector(b, present);
            var arr = DurationArray.CreateDurationArray(b, valuesOff, presentOff);
            return (ValuePayload.DurationArray, arr.Value);
        }

        private static (ValuePayload, int) EncodeStructArray(FlatBufferBuilder b, JsonArray ja, TR.StructTypeRef s, bool[]? present)
        {
            var items = new Offset<StructVal>[ja.Count];
            for (var i = 0; i < ja.Count; i++)
            {
                if (present is not null && !present[i])
                {
                    items[i] = StructVal.CreateStructVal(b);
                    continue;
                }

                var (payloadType, payloadOffset) = EncodeStruct(b, ja[i]!, s);
                if (payloadType != ValuePayload.StructVal)
                {
                    throw new InvalidOperationException("EncodeStruct should always return StructVal.");
                }

                items[i] = new Offset<StructVal>(payloadOffset);
            }

            var itemsVec = StructArray.CreateItemsVector(b, items);
            var presentOff = present is null ? default : StructArray.CreatePresentVector(b, present);
            var arr = StructArray.CreateStructArray(b, itemsVec, presentOff);
            return (ValuePayload.StructArray, arr.Value);
        }
    }
}