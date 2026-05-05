using System;
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
        public static JsonNode ToJsonSchema(this TypeSchema schema)
        {
            return BuildSchema(schema.Type, schema.Annotations, schema.StructFieldAnnotations);
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
            // Recurse into the items type, passing annotations through. The outer BuildSchema's
            // ApplyAnnotations call will also apply annotations to the array node itself —
            // by spec §5.1 convention, x-unit etc. live on both the array and its items.
            var items = BuildSchema(a.Items, ann, sfa);
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
            // Note: BuildArray passes (ann, sfa) through deliberately — the spec §5.1 array convention
            // wants x-unit etc. on both `items` and the array node, which the double-apply produces.
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
        }
    }
}