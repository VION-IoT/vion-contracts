using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Reflection;
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
    ///     the union tag tree is self-describing — while JSON→FB and the CLR-bridge
    ///     directions consume a <see cref="TR.TypeRef" /> schema.
    /// </summary>
    public static class PropertyValueCodec
    {
        private static readonly ConcurrentDictionary<Type, MethodInfo> BuildImmutableArrayCache = new();

        private static readonly Func<Type, MethodInfo> BuildImmutableArrayMethodFactory = elementType =>
                                                                                              typeof(ImmutableArray).GetMethods(BindingFlags.Public | BindingFlags.Static)
                                                                                                                    .Single(m => m.Name == nameof(ImmutableArray.Create) &&
                                                                                                                        m.IsGenericMethodDefinition &&
                                                                                                                        m.GetGenericArguments().Length == 1 &&
                                                                                                                        m.GetParameters().Length == 1 &&
                                                                                                                        m.GetParameters()[0].ParameterType.IsArray)
                                                                                                                    .MakeGenericMethod(elementType);

        /// <summary>
        ///     Decodes a serialized <c>PropertyValue</c> FlatBuffer into a JSON value.
        ///     Returns <c>null</c> when the union payload is <c>NONE</c> (the wire encoding of a null value).
        /// </summary>
        public static JsonNode? FlatBufferToJson(ByteBuffer byteBuffer)
        {
            var pv = PropertyValue.GetRootAsPropertyValue(byteBuffer);
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

        /// <summary>
        ///     Encodes a CLR value into a serialized <c>PropertyValue</c> FlatBuffer using the schema
        ///     <paramref name="type" />. Bridges C# types (including positional record-structs and enums)
        ///     to the wire format by first converting to JSON via <see cref="ClrToJson" />, then to FB
        ///     via <see cref="JsonToFlatBuffer" />.
        /// </summary>
        public static byte[] ClrToFlatBuffer(object? value, TR.TypeRef type)
        {
            var json = ClrToJson(value, type);
            return JsonToFlatBuffer(json, type);
        }

        /// <summary>
        ///     Decodes a serialized <c>PropertyValue</c> FlatBuffer into a CLR value of
        ///     <paramref name="targetClrType" />, using the schema <paramref name="type" /> to bridge
        ///     numeric precision narrowing, enum name→value, struct constructor matching, and
        ///     <see cref="ImmutableArray{T}" /> construction. Returns <c>null</c> when the wire
        ///     payload is <c>NONE</c> and <paramref name="type" /> is <see cref="TR.NullableTypeRef" />.
        /// </summary>
        public static object? FlatBufferToClr(ByteBuffer byteBuffer, TR.TypeRef type, Type targetClrType)
        {
            var json = FlatBufferToJson(byteBuffer);
            return JsonToClr(json, type, targetClrType);
        }

        // ── Validation ────────────────────────────────────────────────────────

        /// <summary>
        ///     Validates <paramref name="json" /> against <paramref name="schema" />, returning every
        ///     mismatch in <see cref="ValidationResult.Errors" />. Checks structural shape (JSON token
        ///     vs <c>TypeRef</c>), numeric range (<see cref="TR.TypeAnnotations.Minimum" /> /
        ///     <see cref="TR.TypeAnnotations.Maximum" />), enum membership
        ///     (<see cref="TR.EnumTypeRef.Members" />), required struct fields, and the property-level
        ///     <see cref="TR.TypeAnnotations.ReadOnly" /> guard. Does not throw on validation failure —
        ///     callers inspect <see cref="ValidationResult.IsValid" />.
        /// </summary>
        public static ValidationResult ValidateJson(JsonNode? json, TR.TypeSchema schema)
        {
            var errors = new List<string>();

            // ReadOnly is a property-level guard: when true, any incoming Set is rejected outright.
            if (schema.Annotations.ReadOnly)
            {
                errors.Add("$: property is read-only.");
                return new ValidationResult(false, errors.ToImmutableArray());
            }

            Validate(json,
                     schema.Type,
                     schema.Annotations,
                     schema.StructFieldAnnotations,
                     "$",
                     errors);
            return errors.Count == 0 ? ValidationResult.Valid : new ValidationResult(false, errors.ToImmutableArray());
        }

        private static object? JsonToClr(JsonNode? json, TR.TypeRef type, Type targetClrType)
        {
            if (type is TR.NullableTypeRef n)
            {
                if (json is null)
                {
                    return null;
                }

                var inner = Nullable.GetUnderlyingType(targetClrType) ?? targetClrType;
                return JsonToClr(json, n.Inner, inner);
            }

            if (json is null)
            {
                throw new PropertyValueDecodeException($"FlatBufferToClr: null wire value but schema '{type.GetType().Name}' is not nullable.");
            }

            return type switch
            {
                TR.PrimitiveTypeRef p => JsonToClrPrimitive(json, p, targetClrType),
                TR.EnumTypeRef _ => EnumNameCache.Parse(targetClrType, json.GetValue<string>()),
                TR.StructTypeRef s => JsonToClrStruct(json, s, targetClrType),
                TR.ArrayTypeRef a => JsonToClrArray(json, a, targetClrType),
                _ => throw new PropertyValueDecodeException($"FlatBufferToClr: unknown schema type '{type.GetType().Name}'."),
            };
        }

        private static object JsonToClrPrimitive(JsonNode json, TR.PrimitiveTypeRef p, Type targetClrType)
        {
            return p.Kind switch
            {
                TR.PrimitiveKind.Bool => json.GetValue<bool>(),
                TR.PrimitiveKind.String => json.GetValue<string>(),

                TR.PrimitiveKind.Byte => RangeCheckedByte(json.GetValue<long>()),
                TR.PrimitiveKind.UShort => RangeCheckedUShort(json.GetValue<long>()),
                TR.PrimitiveKind.UInt => RangeCheckedUInt(json.GetValue<long>()),
                TR.PrimitiveKind.Short => RangeCheckedShort(json.GetValue<long>()),
                TR.PrimitiveKind.Int => RangeCheckedInt(json.GetValue<long>()),
                TR.PrimitiveKind.Long => json.GetValue<long>(),

                TR.PrimitiveKind.Float => (float)json.GetValue<double>(),
                TR.PrimitiveKind.Double => json.GetValue<double>(),

                TR.PrimitiveKind.DateTime => ParseDateTime(json.GetValue<string>()),
                TR.PrimitiveKind.Duration => XmlConvert.ToTimeSpan(json.GetValue<string>()),

                _ => throw new PropertyValueDecodeException($"FlatBufferToClr: unknown PrimitiveKind '{p.Kind}'."),
            };
        }

        private static byte RangeCheckedByte(long v)
        {
            return v is >= byte.MinValue and <= byte.MaxValue ? (byte)v :
                       throw new PropertyValueDecodeException($"FlatBufferToClr: long value {v} out of range for byte (0..255).");
        }

        private static ushort RangeCheckedUShort(long v)
        {
            return v is >= ushort.MinValue and <= ushort.MaxValue ? (ushort)v :
                       throw new PropertyValueDecodeException($"FlatBufferToClr: long value {v} out of range for ushort (0..65535).");
        }

        private static uint RangeCheckedUInt(long v)
        {
            return v is >= 0 and <= uint.MaxValue ? (uint)v : throw new PropertyValueDecodeException($"FlatBufferToClr: long value {v} out of range for uint (0..4294967295).");
        }

        private static short RangeCheckedShort(long v)
        {
            return v is >= short.MinValue and <= short.MaxValue ? (short)v :
                       throw new PropertyValueDecodeException($"FlatBufferToClr: long value {v} out of range for short (-32768..32767).");
        }

        private static int RangeCheckedInt(long v)
        {
            return v is >= int.MinValue and <= int.MaxValue ? (int)v : throw new PropertyValueDecodeException($"FlatBufferToClr: long value {v} out of range for int.");
        }

        private static DateTime ParseDateTime(string s)
        {
            var dto = DateTimeOffset.Parse(s, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return dto.UtcDateTime;
        }

        private static object JsonToClrStruct(JsonNode json, TR.StructTypeRef s, Type targetClrType)
        {
            if (json is not JsonObject obj)
            {
                throw new PropertyValueDecodeException($"FlatBufferToClr: expected JSON object for struct '{s.Title}', got '{json.GetType().Name}'.");
            }

            var ctor = SelectStructConstructor(targetClrType, s);
            var ctorParams = ctor.GetParameters();
            var args = new object?[ctorParams.Length];

            for (var i = 0; i < ctorParams.Length; i++)
            {
                var paramName = ctorParams[i].Name ??
                                throw new PropertyValueDecodeException($"FlatBufferToClr: constructor parameter {i} on '{targetClrType.FullName}' has no name.");
                var schemaFieldName = PascalToCamel(paramName);

                if (!obj.TryGetPropertyValue(schemaFieldName, out var fieldJson))
                {
                    if (s.Required.Contains(schemaFieldName))
                    {
                        throw new PropertyValueDecodeException($"FlatBufferToClr: required field '{schemaFieldName}' missing on struct '{s.Title}'.");
                    }

                    args[i] = null;
                    continue;
                }

                var schemaField = s.Fields.FirstOrDefault(f => f.Name == schemaFieldName) ??
                                  throw new PropertyValueDecodeException($"FlatBufferToClr: schema for struct '{s.Title}' has no field '{schemaFieldName}'.");
                args[i] = JsonToClr(fieldJson, schemaField.Type, ctorParams[i].ParameterType);
            }

            return ctor.Invoke(args);
        }

        private static ConstructorInfo SelectStructConstructor(Type targetClrType, TR.StructTypeRef s)
        {
            var ctors = targetClrType.GetConstructors();
            var matching = ctors.Where(c => c.GetParameters().Length == s.Fields.Length).ToArray();
            if (matching.Length == 0)
            {
                throw new
                    PropertyValueDecodeException($"FlatBufferToClr: '{targetClrType.FullName}' has no public constructor with {s.Fields.Length} parameters (schema '{s.Title}').");
            }

            if (matching.Length > 1)
            {
                throw new
                    PropertyValueDecodeException($"FlatBufferToClr: '{targetClrType.FullName}' has multiple public constructors with {s.Fields.Length} parameters; cannot disambiguate against schema '{s.Title}'.");
            }

            return matching[0];
        }

        private static string PascalToCamel(string pascal)
        {
            return string.IsNullOrEmpty(pascal) ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];
        }

        private static object JsonToClrArray(JsonNode json, TR.ArrayTypeRef a, Type targetClrType)
        {
            if (json is not JsonArray ja)
            {
                throw new PropertyValueDecodeException($"FlatBufferToClr: expected JSON array, got '{json.GetType().Name}'.");
            }

            if (!targetClrType.IsGenericType || targetClrType.GetGenericTypeDefinition() != typeof(ImmutableArray<>))
            {
                throw new PropertyValueDecodeException($"FlatBufferToClr: ArrayTypeRef requires ImmutableArray<T> as target CLR type; got '{targetClrType.FullName}'.");
            }

            var elementType = targetClrType.GetGenericArguments()[0];
            var elements = Array.CreateInstance(elementType, ja.Count);
            for (var i = 0; i < ja.Count; i++)
            {
                elements.SetValue(JsonToClr(ja[i], a.Items, elementType), i);
            }

            return BuildImmutableArrayCache.GetOrAdd(elementType, BuildImmutableArrayMethodFactory).Invoke(null, new object[] { elements })!;
        }

        private static JsonNode? ClrToJson(object? value, TR.TypeRef type)
        {
            // Nullable wrapper handling — C# nullable value types unwrap automatically when boxed
            // (e.g. (object)((int?)null) is null; (object)((int?)42) is 42 boxed as int).
            if (type is TR.NullableTypeRef n)
            {
                if (value is null)
                {
                    return null;
                }

                return ClrToJson(value, n.Inner);
            }

            if (value is null)
            {
                throw new PropertyValueDecodeException($"ClrToFlatBuffer: null value is not valid for non-nullable schema type '{type.GetType().Name}'.");
            }

            return type switch
            {
                TR.PrimitiveTypeRef p => ClrPrimitiveToJson(value, p),
                TR.EnumTypeRef _ => JsonValue.Create(EnumNameCache.GetName(value.GetType(), value)),
                TR.StructTypeRef s => ClrStructToJson(value, s),
                TR.ArrayTypeRef a => ClrArrayToJson(value, a),
                _ => throw new PropertyValueDecodeException($"ClrToFlatBuffer: unknown schema type '{type.GetType().Name}'."),
            };
        }

        private static JsonNode ClrPrimitiveToJson(object value, TR.PrimitiveTypeRef p)
        {
            return p.Kind switch
            {
                TR.PrimitiveKind.Bool => JsonValue.Create((bool)value)!,
                TR.PrimitiveKind.String => JsonValue.Create((string)value)!,

                TR.PrimitiveKind.Byte => JsonValue.Create((long)(byte)value)!,
                TR.PrimitiveKind.UShort => JsonValue.Create((long)(ushort)value)!,
                TR.PrimitiveKind.UInt => JsonValue.Create((long)(uint)value)!,
                TR.PrimitiveKind.Short => JsonValue.Create((long)(short)value)!,
                TR.PrimitiveKind.Int => JsonValue.Create((long)(int)value)!,
                TR.PrimitiveKind.Long => JsonValue.Create((long)value)!,

                TR.PrimitiveKind.Float => JsonValue.Create((double)(float)value)!,
                TR.PrimitiveKind.Double => JsonValue.Create((double)value)!,

                // DateTime: emit as RFC 3339 / ISO 8601 string. Local DateTimes are rejected —
                // the contract requires UTC or Unspecified (treated as UTC).
                TR.PrimitiveKind.DateTime when value is DateTime dt => dt.Kind == DateTimeKind.Local ?
                                                                           throw new
                                                                               PropertyValueDecodeException("ClrToFlatBuffer: DateTime values must be UTC or Unspecified (got Local).") :
                                                                           JsonValue.Create(DateTime.SpecifyKind(dt, DateTimeKind.Utc)
                                                                                                    .ToString("o", CultureInfo.InvariantCulture))!,

                // Duration: ISO 8601 duration via XmlConvert
                TR.PrimitiveKind.Duration when value is TimeSpan ts => JsonValue.Create(XmlConvert.ToString(ts))!,

                _ => throw new PropertyValueDecodeException($"ClrToFlatBuffer: cannot encode CLR value of type '{value.GetType().FullName}' as PrimitiveKind '{p.Kind}'."),
            };
        }

        private static string CamelToPascal(string camelCase)
        {
            return string.IsNullOrEmpty(camelCase) ? camelCase : char.ToUpperInvariant(camelCase[0]) + camelCase[1..];
        }

        private static JsonObject ClrStructToJson(object value, TR.StructTypeRef s)
        {
            var obj = new JsonObject();
            var clrType = value.GetType();
            foreach (var field in s.Fields)
            {
                var pascalName = CamelToPascal(field.Name);
                var prop = clrType.GetProperty(pascalName) ??
                           throw new
                               PropertyValueDecodeException($"ClrToFlatBuffer: CLR type '{clrType.FullName}' has no property '{pascalName}' for schema field '{field.Name}' on struct '{s.Title}'.");
                var fieldValue = prop.GetValue(value);
                obj[field.Name] = ClrToJson(fieldValue, field.Type);
            }

            return obj;
        }

        private static JsonArray ClrArrayToJson(object value, TR.ArrayTypeRef a)
        {
            if (value is not IEnumerable enumerable)
            {
                throw new PropertyValueDecodeException($"ClrToFlatBuffer: expected IEnumerable for ArrayTypeRef, got '{value.GetType().FullName}'.");
            }

            var arr = new JsonArray();
            foreach (var item in enumerable)
            {
                arr.Add(ClrToJson(item, a.Items));
            }

            return arr;
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

        private static void Validate(JsonNode? json,
                                     TR.TypeRef type,
                                     TR.TypeAnnotations annot,
                                     ImmutableDictionary<string, TR.TypeAnnotations> fieldAnnotations,
                                     string path,
                                     List<string> errors)
        {
            if (type is TR.NullableTypeRef n)
            {
                if (json is null)
                {
                    return; // null is valid for nullable
                }

                Validate(json,
                         n.Inner,
                         annot,
                         fieldAnnotations,
                         path,
                         errors);
                return;
            }

            if (json is null)
            {
                errors.Add($"{path}: null is not valid for non-nullable type.");
                return;
            }

            switch (type)
            {
                case TR.PrimitiveTypeRef p:
                    ValidatePrimitive(json, p, annot, path, errors);
                    break;
                case TR.EnumTypeRef e:
                    ValidateEnum(json, e, path, errors);
                    break;
                case TR.StructTypeRef s:
                    ValidateStruct(json, s, fieldAnnotations, path, errors);
                    break;
                case TR.ArrayTypeRef a:
                    ValidateArray(json, a, fieldAnnotations, path, errors);
                    break;
                default:
                    errors.Add($"{path}: unknown schema type '{type.GetType().Name}'.");
                    break;
            }
        }

        private static void ValidatePrimitive(JsonNode json, TR.PrimitiveTypeRef p, TR.TypeAnnotations annot, string path, List<string> errors)
        {
            if (json is not JsonValue jv)
            {
                errors.Add($"{path}: expected JSON scalar for PrimitiveKind '{p.Kind}', got '{json.GetType().Name}'.");
                return;
            }

            switch (p.Kind)
            {
                case TR.PrimitiveKind.Bool:
                    if (!jv.TryGetValue<bool>(out _))
                    {
                        errors.Add($"{path}: expected JSON bool for PrimitiveKind 'Bool', got '{jv}'.");
                    }

                    break;
                case TR.PrimitiveKind.String:
                    if (!jv.TryGetValue<string>(out _))
                    {
                        errors.Add($"{path}: expected JSON string for PrimitiveKind 'String', got '{jv}'.");
                    }

                    break;
                case TR.PrimitiveKind.Byte:
                case TR.PrimitiveKind.Short:
                case TR.PrimitiveKind.UShort:
                case TR.PrimitiveKind.Int:
                case TR.PrimitiveKind.UInt:
                case TR.PrimitiveKind.Long:
                    if (!jv.TryGetValue<long>(out var lv))
                    {
                        errors.Add($"{path}: expected JSON integer for PrimitiveKind '{p.Kind}', got '{jv}'.");
                        break;
                    }

                    CheckIntegerRange(lv, p.Kind, path, errors);
                    CheckMinMax(lv, annot, path, errors);
                    break;
                case TR.PrimitiveKind.Float:
                case TR.PrimitiveKind.Double:
                    if (!jv.TryGetValue<double>(out var dv))
                    {
                        errors.Add($"{path}: expected JSON number for PrimitiveKind '{p.Kind}', got '{jv}'.");
                        break;
                    }

                    CheckMinMax(dv, annot, path, errors);
                    break;
                case TR.PrimitiveKind.DateTime:
                    if (!jv.TryGetValue<string>(out var dts) || !DateTimeOffset.TryParse(dts, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out _))
                    {
                        errors.Add($"{path}: expected RFC 3339 / ISO 8601 string for PrimitiveKind 'DateTime', got '{jv}'.");
                    }

                    break;
                case TR.PrimitiveKind.Duration:
                    if (!jv.TryGetValue<string>(out var ds))
                    {
                        errors.Add($"{path}: expected ISO 8601 duration string for PrimitiveKind 'Duration', got '{jv}'.");
                        break;
                    }

                    try
                    {
                        _ = XmlConvert.ToTimeSpan(ds);
                    }
                    catch (FormatException)
                    {
                        errors.Add($"{path}: '{ds}' is not a valid ISO 8601 duration.");
                    }

                    break;
                default:
                    errors.Add($"{path}: unknown PrimitiveKind '{p.Kind}'.");
                    break;
            }
        }

        private static void CheckIntegerRange(long v, TR.PrimitiveKind kind, string path, List<string> errors)
        {
            var (min, max) = kind switch
            {
                TR.PrimitiveKind.Byte => (byte.MinValue, byte.MaxValue),
                TR.PrimitiveKind.UShort => (ushort.MinValue, ushort.MaxValue),
                TR.PrimitiveKind.UInt => (0L, uint.MaxValue),
                TR.PrimitiveKind.Short => (short.MinValue, short.MaxValue),
                TR.PrimitiveKind.Int => ((long)int.MinValue, (long)int.MaxValue),
                TR.PrimitiveKind.Long => (long.MinValue, long.MaxValue),
                _ => (long.MinValue, long.MaxValue),
            };
            if (v < min || v > max)
            {
                errors.Add($"{path}: integer {v} is out of range for PrimitiveKind '{kind}' ({min}..{max}).");
            }
        }

        private static void CheckMinMax(double v, TR.TypeAnnotations annot, string path, List<string> errors)
        {
            if (annot.Minimum is double min && v < min)
            {
                errors.Add($"{path}: value {v} is below minimum {min}.");
            }

            if (annot.Maximum is double max && v > max)
            {
                errors.Add($"{path}: value {v} is above maximum {max}.");
            }
        }

        private static void ValidateEnum(JsonNode json, TR.EnumTypeRef e, string path, List<string> errors)
        {
            if (json is not JsonValue jv || !jv.TryGetValue<string>(out var name))
            {
                errors.Add($"{path}: expected JSON string for enum '{e.Title}', got '{json.GetType().Name}'.");
                return;
            }

            if (!e.Members.Contains(name))
            {
                errors.Add($"{path}: '{name}' is not a member of enum '{e.Title}' (allowed: {string.Join(", ", e.Members)}).");
            }
        }

        private static void ValidateStruct(JsonNode json, TR.StructTypeRef s, ImmutableDictionary<string, TR.TypeAnnotations> fieldAnnotations, string path, List<string> errors)
        {
            if (json is not JsonObject obj)
            {
                errors.Add($"{path}: expected JSON object for struct '{s.Title}', got '{json.GetType().Name}'.");
                return;
            }

            // Required fields must be present
            foreach (var requiredName in s.Required)
            {
                if (!obj.ContainsKey(requiredName))
                {
                    errors.Add($"{path}: required field '{requiredName}' missing on struct '{s.Title}'.");
                }
            }

            // Validate each present field (against schema's known fields)
            foreach (var field in s.Fields)
            {
                if (!obj.TryGetPropertyValue(field.Name, out var fieldJson))
                {
                    continue; // already reported above if required; silently OK if optional
                }

                var fieldAnnot = fieldAnnotations.TryGetValue(field.Name, out var fa) ? fa : TR.TypeAnnotations.None;
                Validate(fieldJson,
                         field.Type,
                         fieldAnnot,
                         ImmutableDictionary<string, TR.TypeAnnotations>.Empty,
                         $"{path}.{field.Name}",
                         errors);
            }

            // Note: spec says additionalProperties: false. Report unknown keys.
            foreach (var kvp in obj)
            {
                if (!s.Fields.Any(f => f.Name == kvp.Key))
                {
                    errors.Add($"{path}: unknown field '{kvp.Key}' on struct '{s.Title}' (additionalProperties not allowed).");
                }
            }
        }

        private static void ValidateArray(JsonNode json, TR.ArrayTypeRef a, ImmutableDictionary<string, TR.TypeAnnotations> fieldAnnotations, string path, List<string> errors)
        {
            if (json is not JsonArray ja)
            {
                errors.Add($"{path}: expected JSON array, got '{json.GetType().Name}'.");
                return;
            }

            for (var i = 0; i < ja.Count; i++)
            {
                Validate(ja[i],
                         a.Items,
                         TR.TypeAnnotations.None,
                         fieldAnnotations,
                         $"{path}[{i}]",
                         errors);
            }
        }
    }
}
