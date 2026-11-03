using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;

namespace Vion.Contracts.Test.Events
{
    /// <summary>
    ///     Rewrites one payload's exported schema so every node tagged as a named type becomes a <c>#/$defs/&lt;key&gt;</c>
    ///     reference, its definition hoisted into the shared <c>$defs</c>. The exporter's pointer <c>$ref</c>s
    ///     (<c>#/properties/…</c>) are resolved against the payload's tagged schema first, so none survives.
    ///     <para>
    ///         A definition is the type's schema without <c>null</c>; where a use allows <c>null</c>, the reference is
    ///         <c>{"anyOf": [{"$ref": …}, {"type": "null"}]}</c>. A type met again inside its own definition is a reference,
    ///         which is how a recursive type is expressed. A type met again anywhere else is rewritten again and must
    ///         produce the same definition, or the export fails.
    ///     </para>
    /// </summary>
    internal sealed class DefinitionHoister
    {
        private const string DefinitionTag = "x-vion-definition";

        private const string DefinitionsPrefix = "#/$defs/";

        private readonly IDictionary<string, JsonObject> _definitions;

        private readonly HashSet<string> _expanding = [];

        private readonly JsonNode _exported;

        public DefinitionHoister(JsonNode exported, IDictionary<string, JsonObject> definitions)
        {
            _exported = exported;
            _definitions = definitions;
        }

        public JsonNode Hoist()
        {
            return Rewrite(_exported)!;
        }

        private JsonNode? Rewrite(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject schema:
                    return RewriteObject(schema);
                case JsonArray array:
                    return new JsonArray(array.Select(Rewrite).ToArray());
                default:
                    return node?.DeepClone();
            }
        }

        private JsonNode RewriteObject(JsonObject schema)
        {
            if (schema["$ref"] is JsonValue pointer)
            {
                return Rewrite(Resolve((string)pointer!));
            }

            var key = (string?)schema[DefinitionTag];
            if (key is not null && _expanding.Contains(key))
            {
                return Reference(key, AllowsNull(schema));
            }

            if (key is not null)
            {
                _expanding.Add(key);
            }

            var copy = new JsonObject();
            foreach (var member in schema)
            {
                if (member.Key != DefinitionTag)
                {
                    copy[member.Key] = Rewrite(member.Value);
                }
            }

            if (key is null)
            {
                return copy;
            }

            _expanding.Remove(key);

            var nullable = RemoveNull(copy);
            if (!_definitions.TryGetValue(key, out var existing))
            {
                _definitions[key] = copy;
            }
            else if (!JsonNode.DeepEquals(existing, copy))
            {
                throw new InvalidOperationException($"Two different schemas for {key}: {existing.ToJsonString()} and {copy.ToJsonString()}.");
            }

            return Reference(key, nullable);
        }

        private JsonNode Resolve(string pointer)
        {
            if (!pointer.StartsWith("#/", StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Cannot resolve the exporter's $ref '{pointer}' inside the payload schema.");
            }

            var node = _exported;
            foreach (var segment in pointer.Substring(2).Split('/'))
            {
                var name = segment.Replace("~1", "/").Replace("~0", "~");
                node = node is JsonArray array ? array[int.Parse(name)]! : node[name]!;
            }

            return node;
        }

        private static JsonObject Reference(string key, bool nullable)
        {
            var reference = new JsonObject { ["$ref"] = DefinitionsPrefix + key };
            if (!nullable)
            {
                return reference;
            }

            return new JsonObject { ["anyOf"] = new JsonArray(reference, new JsonObject { ["type"] = "null" }) };
        }

        private static bool AllowsNull(JsonObject schema)
        {
            var types = schema["type"] as JsonArray;
            var values = schema["enum"] as JsonArray;

            return types?.Any(IsNullType) == true || values?.Any(value => value is null) == true;
        }

        // Takes null out of the definition itself; the caller puts it back on the reference.
        private static bool RemoveNull(JsonObject schema)
        {
            var nullable = AllowsNull(schema);

            if (schema["type"] is JsonArray types && types.Any(IsNullType))
            {
                var rest = types.Where(type => !IsNullType(type)).Select(type => (string)type!).ToList();
                schema["type"] = rest.Count == 1 ? JsonValue.Create(rest[0]) : new JsonArray(rest.Select(type => (JsonNode)JsonValue.Create(type)).ToArray());
            }

            if (schema["enum"] is JsonArray values && values.Any(value => value is null))
            {
                schema["enum"] = new JsonArray(values.Where(value => value is not null).Select(value => value!.DeepClone()).ToArray());
            }

            return nullable;
        }

        private static bool IsNullType(JsonNode? type)
        {
            return type is JsonValue value && (string?)value == "null";
        }
    }
}
