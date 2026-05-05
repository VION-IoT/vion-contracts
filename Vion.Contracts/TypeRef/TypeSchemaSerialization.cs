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
            // Build methods that recurse (BuildArray, BuildNullable) pass annotations down so the
            // recursive BuildSchema call can apply them at the inner level. Build methods that don't
            // recurse (BuildPrimitive, BuildEnum, BuildStruct) accept only the data they need to
            // construct the node — annotations are layered on top here.
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

        // Stubs filled in by later tasks. Throwing NotImplementedException keeps the build green
        // while making misuse loud — only callers that actually exercise these branches will fault.
        private static JsonObject BuildStruct(StructTypeRef s, ImmutableDictionary<string, TypeAnnotations> sfa)
        {
            throw new NotImplementedException("Struct serialization arrives in §2.10");
        }

        private static JsonObject BuildArray(ArrayTypeRef a, TypeAnnotations ann, ImmutableDictionary<string, TypeAnnotations> sfa)
        {
            throw new NotImplementedException("Array serialization arrives in §2.11");
        }

        private static JsonObject BuildNullable(NullableTypeRef n, TypeAnnotations ann, ImmutableDictionary<string, TypeAnnotations> sfa)
        {
            throw new NotImplementedException("Nullable serialization arrives in §2.12");
        }

        private static void ApplyAnnotations(JsonNode node, TypeAnnotations ann)
        {
            // Filled in by §2.13. No-op until then; primitive tests don't depend on annotations.
        }
    }
}