using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Codec helper: serialises a <see cref="TypeSchema" /> into a JSON Schema 2020-12 document
    ///     (Dale profile, see spec §5.1). Walks the <see cref="TypeRef" /> tree dispatching by kind,
    ///     then layers annotations on top. The reverse direction (<c>FromJsonSchema</c>) lands in §2.14.
    /// </summary>
    public static class TypeSchemaSerialization
    {
        // ── FromJsonSchema (§2.14) ──────────────────────────────────────────────────────────────────

        private static readonly HashSet<string> AllowedKeywords = new()
                                                                  {
                                                                      "type", "format", "title", "description",
                                                                      "minimum", "maximum",
                                                                      "x-unit", "x-kind",
                                                                      "readOnly", "writeOnly",
                                                                      "enum",
                                                                      "properties", "required", "additionalProperties",
                                                                      "items",
                                                                  };

        public static JsonNode ToJsonSchema(this TypeSchema schema)
        {
            return BuildSchema(schema.Type, schema.Annotations, schema.StructFieldAnnotations);
        }

        public static TypeSchema FromJsonSchema(JsonNode node)
        {
            var (type, annotations, sfa) = ParseNode(node);
            return new TypeSchema(type, annotations, sfa);
        }

        private static JsonNode BuildSchema(TypeRef type, TypeAnnotations annotations, ImmutableDictionary<string, TypeAnnotations> structFieldAnnotations)
        {
            var node = type switch
            {
                PrimitiveTypeRef p => BuildPrimitive(p),
                EnumTypeRef e => BuildEnum(e),
                StructTypeRef s => BuildStruct(s, structFieldAnnotations),
                ArrayTypeRef a => BuildArray(a, annotations, structFieldAnnotations),
                NullableTypeRef n => BuildNullable(n, annotations, structFieldAnnotations),
                _ => throw new InvalidOperationException($"Unknown TypeRef: {type.GetType()}"),
            };

            // Annotations are applied centrally here (after dispatch), not inside each Build method.
            // - BuildPrimitive, BuildEnum, BuildStruct don't recurse — they construct the node and
            //   ApplyAnnotations layers the annotations on top here.
            // - BuildArray recurses with (ann, sfa) passed through. The inner BuildSchema applies
            //   annotations to `items`, and this outer call applies them again to the array node.
            //   That double-apply is intentional per spec §5.1 (x-unit lives on both array and items).
            // - BuildNullable recurses with empty (ann, sfa) so annotations are applied exactly once,
            //   here, on the mutated inner JsonObject — see comment in BuildNullable for rationale.
            ApplyAnnotations(node, annotations);
            return node;
        }

        private static JsonObject BuildPrimitive(PrimitiveTypeRef p)
        {
            return p.Kind switch
            {
                PrimitiveKind.Bool => new JsonObject { ["type"] = "boolean" },
                PrimitiveKind.String => new JsonObject { ["type"] = "string" },
                PrimitiveKind.Byte => new JsonObject { ["type"] = "integer", ["format"] = "uint8" },
                PrimitiveKind.Short => new JsonObject { ["type"] = "integer", ["format"] = "int16" },
                PrimitiveKind.UShort => new JsonObject { ["type"] = "integer", ["format"] = "uint16" },
                PrimitiveKind.Int => new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                PrimitiveKind.UInt => new JsonObject { ["type"] = "integer", ["format"] = "uint32" },
                PrimitiveKind.Long => new JsonObject { ["type"] = "integer", ["format"] = "int64" },
                PrimitiveKind.Float => new JsonObject { ["type"] = "number", ["format"] = "float" },
                PrimitiveKind.Double => new JsonObject { ["type"] = "number", ["format"] = "double" },
                PrimitiveKind.DateTime => new JsonObject { ["type"] = "string", ["format"] = "date-time" },
                PrimitiveKind.Duration => new JsonObject { ["type"] = "string", ["format"] = "duration" },
                _ => throw new InvalidOperationException($"Unknown PrimitiveKind: {p.Kind}"),
            };
        }

        private static JsonObject BuildEnum(EnumTypeRef e)
        {
            var members = new JsonArray();
            foreach (var m in e.Members)
            {
                members.Add(m);
            }

            return new JsonObject
                   {
                       ["type"] = "string",
                       ["title"] = e.Title,
                       ["enum"] = members,
                   };
        }

        private static JsonObject BuildStruct(StructTypeRef s, ImmutableDictionary<string, TypeAnnotations> structFieldAnnotations)
        {
            var properties = new JsonObject();
            foreach (var f in s.Fields)
            {
                var fieldAnnotations = structFieldAnnotations.TryGetValue(f.Name, out var a) ? a : TypeAnnotations.None;
                var fieldNode = BuildSchema(f.Type, fieldAnnotations, ImmutableDictionary<string, TypeAnnotations>.Empty);
                properties[f.Name] = fieldNode;
            }

            var required = new JsonArray();
            foreach (var r in s.Required)
            {
                required.Add(r);
            }

            return new JsonObject
                   {
                       ["type"] = "object",
                       ["title"] = s.Title,
                       ["properties"] = properties,
                       ["required"] = required,
                       ["additionalProperties"] = false,
                   };
        }

        private static JsonObject BuildArray(ArrayTypeRef a, TypeAnnotations ann, ImmutableDictionary<string, TypeAnnotations> sfa)
        {
            // Property-level annotations (title, x-unit, readOnly) belong on the array root
            // only, not the items subschema — `items` describes the element SHAPE; property-level
            // labels and units describe the property as a whole. The outer BuildSchema's
            // ApplyAnnotations call applies them to the array node we return.
            //
            // StructFieldAnnotations (sfa) DO pass through, because for array-of-struct the per-field
            // [StructField] data needs to reach the inner struct's properties (e.g. Lat/Lon with
            // their own x-unit "°"). That's an element-shape concern, not a property concern.
            var items = BuildSchema(a.Items, TypeAnnotations.None, sfa);
            return new JsonObject
                   {
                       ["type"] = "array",
                       ["items"] = items,
                   };
        }

        private static JsonObject BuildNullable(NullableTypeRef n, TypeAnnotations ann, ImmutableDictionary<string, TypeAnnotations> sfa)
        {
            // Pass empty annotations / SFA into the recursive BuildSchema. Annotations belong to the
            // OUTER property-level call site, not the inner unwrapped type — the outer BuildSchema's
            // ApplyAnnotations runs exactly once on this same JsonObject after we return. If we passed
            // (ann, sfa) here, ApplyAnnotations would fire twice on the same object (the inner
            // BuildSchema would apply them, and the outer one would apply them again). Currently a
            // no-op since §2.13 hasn't landed; setting up the right shape now avoids latent bugs.
            // Note: BuildArray passes only sfa (NOT ann) through — property-level annotations
            // belong on the array root only; the items subschema carries element-shape concerns.
            var inner = BuildSchema(n.Inner, TypeAnnotations.None, ImmutableDictionary<string, TypeAnnotations>.Empty);
            if (inner is not JsonObject obj)
            {
                throw new InvalidOperationException($"Unexpected schema node: {inner.GetType()}");
            }

            // Widen "type": "X" → ["X", "null"].
            if (obj["type"] is JsonValue v && v.TryGetValue<string>(out var typeStr))
            {
                obj["type"] = new JsonArray(typeStr, "null");
            }

            // For enums, append null to the "enum" array.
            if (obj["enum"] is JsonArray enumArr)
            {
                var copy = new JsonArray();
                foreach (var e in enumArr)
                {
                    copy.Add(e?.DeepClone());
                }

                copy.Add(null);
                obj["enum"] = copy;
            }

            return obj;
        }

        private static void ApplyAnnotations(JsonNode node, TypeAnnotations ann)
        {
            if (node is not JsonObject obj)
            {
                return;
            }

            // Don't overwrite identity-bearing titles set by BuildEnum or BuildStruct.
            if (ann.Title is not null && obj["title"] is null)
            {
                obj["title"] = ann.Title;
            }

            if (ann.Description is not null)
            {
                obj["description"] = ann.Description;
            }

            if (ann.Minimum is double mn)
            {
                obj["minimum"] = mn;
            }

            if (ann.Maximum is double mx)
            {
                obj["maximum"] = mx;
            }

            if (ann.Unit is not null)
            {
                obj["x-unit"] = ann.Unit;
            }

            if (ann.ReadOnly)
            {
                obj["readOnly"] = true;
            }

            if (ann.WriteOnly)
            {
                obj["writeOnly"] = true;
            }

            if (ann.Kind is { } kind)
            {
                obj["x-kind"] = KindToWire(kind);
            }
        }

        private static string KindToWire(MeasuringPointKind kind)
        {
            return kind switch
            {
                MeasuringPointKind.Measurement => "measurement",
                MeasuringPointKind.Total => "total",
                MeasuringPointKind.TotalIncreasing => "totalIncreasing",
                _ => throw new InvalidOperationException($"Unknown MeasuringPointKind: {kind}"),
            };
        }

        private static MeasuringPointKind? KindFromWire(string? wire)
        {
            return wire switch
            {
                null => null,
                "measurement" => MeasuringPointKind.Measurement,
                "total" => MeasuringPointKind.Total,
                "totalIncreasing" => MeasuringPointKind.TotalIncreasing,
                _ => throw new InvalidSchemaException($"Unknown x-kind value: '{wire}'"),
            };
        }

        private static (TypeRef, TypeAnnotations, ImmutableDictionary<string, TypeAnnotations>) ParseNode(JsonNode node)
        {
            if (node is not JsonObject obj)
            {
                throw new InvalidSchemaException("Schema must be a JSON object");
            }

            foreach (var kv in obj)
            {
                if (!AllowedKeywords.Contains(kv.Key))
                {
                    throw new InvalidSchemaException($"Disallowed keyword in Dale profile: '{kv.Key}'");
                }
            }

            var (typeStr, isNullable) = ParseTypeKeyword(obj);
            var annotations = ParseAnnotations(obj);
            var (typeRef, sfa) = typeStr switch
            {
                "boolean" => (new PrimitiveTypeRef(PrimitiveKind.Bool), ImmutableDictionary<string, TypeAnnotations>.Empty),
                "string" => ParseString(obj),
                "integer" => (ParsePrimitiveInteger(obj), ImmutableDictionary<string, TypeAnnotations>.Empty),
                "number" => (ParsePrimitiveNumber(obj), ImmutableDictionary<string, TypeAnnotations>.Empty),
                "object" => ParseObject(obj),
                "array" => ParseArray(obj),
                _ => throw new InvalidSchemaException($"Unknown type: '{typeStr}'"),
            };

            // For nominal types (enum/struct), Title is identity-bearing on the TypeRef —
            // don't double up by leaving it in annotations too. Mirrors the BuildEnum/BuildStruct
            // paths that set obj["title"] directly.
            if (typeRef is EnumTypeRef or StructTypeRef)
            {
                annotations = annotations with { Title = null };
            }

            // writeOnly only applies to string (or string?) in v1 — nullable wrapping happens after
            // this check, so the bare typeRef is the unwrapped element type.
            if (annotations.WriteOnly && typeRef is not PrimitiveTypeRef { Kind: PrimitiveKind.String })
            {
                throw new InvalidSchemaException("writeOnly is only supported on string or string? schemas in v1");
            }

            return (isNullable ? new NullableTypeRef(typeRef) : typeRef, annotations, sfa);
        }

        private static (string, bool) ParseTypeKeyword(JsonObject obj)
        {
            var t = obj["type"] ?? throw new InvalidSchemaException("Missing 'type' keyword");
            if (t is JsonValue v && v.TryGetValue<string>(out var s))
            {
                return (s, false);
            }

            if (t is JsonArray arr && arr.Count == 2)
            {
                var first = arr[0]?.GetValue<string>();
                var second = arr[1]?.GetValue<string>();
                if (second == "null" && first is not null)
                {
                    return (first, true);
                }

                if (first == "null" && second is not null)
                {
                    return (second, true);
                }
            }

            throw new InvalidSchemaException("Unsupported 'type' shape; allowed: string or [X, \"null\"]");
        }

        private static TypeAnnotations ParseAnnotations(JsonObject obj)
        {
            return new TypeAnnotations
                   {
                       Title = obj["title"]?.GetValue<string>(),
                       Description = obj["description"]?.GetValue<string>(),
                       Unit = obj["x-unit"]?.GetValue<string>(),
                       Minimum = obj["minimum"]?.GetValue<double>(),
                       Maximum = obj["maximum"]?.GetValue<double>(),
                       ReadOnly = obj["readOnly"]?.GetValue<bool>() ?? false,
                       WriteOnly = obj["writeOnly"]?.GetValue<bool>() ?? false,
                       Kind = KindFromWire(obj["x-kind"]?.GetValue<string>()),
                   };
        }

        private static TypeRef ParsePrimitiveInteger(JsonObject obj)
        {
            return obj["format"]?.GetValue<string>() switch
            {
                "uint8" => new PrimitiveTypeRef(PrimitiveKind.Byte),
                "int16" => new PrimitiveTypeRef(PrimitiveKind.Short),
                "uint16" => new PrimitiveTypeRef(PrimitiveKind.UShort),
                "int32" => new PrimitiveTypeRef(PrimitiveKind.Int),
                "uint32" => new PrimitiveTypeRef(PrimitiveKind.UInt),
                "int64" => new PrimitiveTypeRef(PrimitiveKind.Long),
                null => new PrimitiveTypeRef(PrimitiveKind.Int), // default when format omitted
                var f => throw new InvalidSchemaException($"Unsupported integer format: '{f}'"),
            };
        }

        private static TypeRef ParsePrimitiveNumber(JsonObject obj)
        {
            return obj["format"]?.GetValue<string>() switch
            {
                "float" => new PrimitiveTypeRef(PrimitiveKind.Float),
                "double" => new PrimitiveTypeRef(PrimitiveKind.Double),
                null => new PrimitiveTypeRef(PrimitiveKind.Double), // default when format omitted
                var f => throw new InvalidSchemaException($"Unsupported number format: '{f}'"),
            };
        }

        private static (TypeRef, ImmutableDictionary<string, TypeAnnotations>) ParseString(JsonObject obj)
        {
            if (obj["enum"] is JsonArray enumArr)
            {
                var members = ImmutableArray.CreateBuilder<string>();
                foreach (var e in enumArr)
                {
                    if (e is null)
                    {
                        continue; // null appears in nullable enum; the wrapping NullableTypeRef captures it
                    }

                    if (!e.AsValue().TryGetValue<string>(out var name))
                    {
                        throw new InvalidSchemaException("Enum members must be strings");
                    }

                    members.Add(name);
                }

                var title = obj["title"]?.GetValue<string>() ?? throw new InvalidSchemaException("Enum schema must include 'title'");
                return (new EnumTypeRef(title, members.ToImmutable()), ImmutableDictionary<string, TypeAnnotations>.Empty);
            }

            return obj["format"]?.GetValue<string>() switch
            {
                "date-time" => (new PrimitiveTypeRef(PrimitiveKind.DateTime), ImmutableDictionary<string, TypeAnnotations>.Empty),
                "duration" => (new PrimitiveTypeRef(PrimitiveKind.Duration), ImmutableDictionary<string, TypeAnnotations>.Empty),
                null => (new PrimitiveTypeRef(PrimitiveKind.String), ImmutableDictionary<string, TypeAnnotations>.Empty),
                var f => throw new InvalidSchemaException($"Unsupported string format: '{f}'"),
            };
        }

        private static (TypeRef, ImmutableDictionary<string, TypeAnnotations>) ParseObject(JsonObject obj)
        {
            var ap = obj["additionalProperties"];
            if (ap is null || (ap is JsonValue av && av.TryGetValue<bool>(out var b) && !b))
            {
                // ok — additionalProperties absent or explicitly false
            }
            else
            {
                throw new InvalidSchemaException("Struct must declare additionalProperties: false");
            }

            var title = obj["title"]?.GetValue<string>() ?? throw new InvalidSchemaException("Struct schema must include 'title'");

            var props = obj["properties"] as JsonObject ?? throw new InvalidSchemaException("Struct schema must include 'properties'");
            var requiredArr = obj["required"] as JsonArray ?? new JsonArray();

            var fields = ImmutableArray.CreateBuilder<StructField>();
            var sfaBuilder = ImmutableDictionary.CreateBuilder<string, TypeAnnotations>();
            foreach (var kv in props)
            {
                var fieldNode = kv.Value!;
                var (fieldType, fieldAnn, _) = ParseNode(fieldNode);
                fields.Add(new StructField(kv.Key, fieldType));
                if (fieldAnn != TypeAnnotations.None)
                {
                    sfaBuilder[kv.Key] = fieldAnn;
                }
            }

            var required = ImmutableArray.CreateBuilder<string>();
            foreach (var r in requiredArr)
            {
                required.Add(r!.GetValue<string>());
            }

            return (new StructTypeRef(title, fields.ToImmutable(), required.ToImmutable()), sfaBuilder.ToImmutable());
        }

        private static (TypeRef, ImmutableDictionary<string, TypeAnnotations>) ParseArray(JsonObject obj)
        {
            var items = obj["items"] ?? throw new InvalidSchemaException("Array schema must include 'items'");

            // Property-level annotations (title, x-unit, readOnly) belong on the array root
            // — items only carries element-shape concerns (type, format, enum, struct properties,
            // nullable type-array). The producer side (BuildArray) emits them only on the array
            // root, so for our own schemas no item-level annotations are expected here. If a
            // third-party schema source puts them on items, we ignore them; the array root is
            // the only authoritative location.
            var (itemType, _itemAnnotationsIgnored, sfa) = ParseNode(items);
            if (itemType is ArrayTypeRef)
            {
                throw new InvalidSchemaException("Nested arrays are not supported in the Dale profile");
            }

            return (new ArrayTypeRef(itemType), sfa);
        }
    }
}
