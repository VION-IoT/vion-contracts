using System.Collections.Immutable;
using System.Linq;
using System.Text.Json.Nodes;
using Vion.Contracts.Conventions;
using Vion.Contracts.TypeRef;

namespace Vion.Contracts.Codec
{
    /// <summary>
    ///     Enforces the platform's WriteOnly wire semantics at publish and set boundaries, so consumers only ever
    ///     handle real values.
    /// </summary>
    /// <remarks>
    ///     Outbound state publishes redact WriteOnly positions to
    ///     <see cref="WriteOnlyConventions.RedactedSentinel" />; inbound updates that echo the sentinel for a secret
    ///     the client didn't change keep the stored value.
    /// </remarks>
    public static class WriteOnlyCodec
    {
        /// <summary>
        ///     Returns <paramref name="value" /> with every WriteOnly position replaced by the redaction sentinel.
        /// </summary>
        /// <remarks>
        ///     Redacts the whole value when the schema itself is WriteOnly, otherwise each set WriteOnly struct
        ///     member — per item when the value is an array of structs. A null position stays null rather than
        ///     becoming the sentinel, so a client can still tell an empty secret from a stored, hidden one. The
        ///     input is never mutated; a schema with WriteOnly members yields one clone.
        /// </remarks>
        /// <param name="schema">The field's schema, carrying the WriteOnly annotations.</param>
        /// <param name="value">The field value about to be published.</param>
        /// <returns>The value safe for broadcast.</returns>
        public static JsonNode? Redact(TypeSchema schema, JsonNode? value)
        {
            if (schema.Annotations.WriteOnly)
            {
                return value == null ? null : JsonValue.Create(WriteOnlyConventions.RedactedSentinel);
            }

            if (value == null || !schema.StructFieldAnnotations.Any(member => member.Value.WriteOnly))
            {
                return value;
            }

            var redacted = value.DeepClone();
            Redact(schema.StructFieldAnnotations, redacted);

            return redacted;
        }

        /// <summary>
        ///     Returns <paramref name="incoming" /> with every WriteOnly position that holds the redaction sentinel
        ///     replaced by the corresponding value from <paramref name="current" />.
        /// </summary>
        /// <remarks>
        ///     Clients that round-trip a whole value echo the sentinel for secrets they didn't change — for those
        ///     positions the stored value is kept. A null position stays null (an explicit clear). Array items
        ///     resolve positionally: the incoming array defines the new length and order, and an item's sentinel
        ///     takes the current value at the same index. A sentinel with no current counterpart resolves to null —
        ///     the sentinel itself never passes through to the write. The inputs are never mutated; a schema with
        ///     WriteOnly members yields one clone.
        /// </remarks>
        /// <param name="schema">The field's schema, carrying the WriteOnly annotations.</param>
        /// <param name="incoming">The value received from the wire.</param>
        /// <param name="current">The field's current value, read from the stored state.</param>
        /// <returns>The value to apply to the state.</returns>
        public static JsonNode? Resolve(TypeSchema schema, JsonNode? incoming, JsonNode? current)
        {
            if (schema.Annotations.WriteOnly)
            {
                return IsSentinel(incoming) ? current?.DeepClone() : incoming;
            }

            if (incoming == null || !schema.StructFieldAnnotations.Any(member => member.Value.WriteOnly))
            {
                return incoming;
            }

            var resolved = incoming.DeepClone();
            Resolve(schema.StructFieldAnnotations, resolved, current);

            return resolved;
        }

        private static void Redact(ImmutableDictionary<string, TypeAnnotations> memberAnnotations, JsonNode value)
        {
            switch (value)
            {
                case JsonObject members:
                    foreach (var (name, annotations) in memberAnnotations)
                    {
                        if (annotations.WriteOnly && members[name] != null)
                        {
                            members[name] = JsonValue.Create(WriteOnlyConventions.RedactedSentinel);
                        }
                    }

                    break;
                case JsonArray items:
                    foreach (var item in items)
                    {
                        if (item != null)
                        {
                            Redact(memberAnnotations, item);
                        }
                    }

                    break;
            }
        }

        private static void Resolve(ImmutableDictionary<string, TypeAnnotations> memberAnnotations, JsonNode incoming, JsonNode? current)
        {
            switch (incoming)
            {
                case JsonObject incomingMembers:
                    foreach (var (name, annotations) in memberAnnotations)
                    {
                        if (annotations.WriteOnly && IsSentinel(incomingMembers[name]))
                        {
                            incomingMembers[name] = current is JsonObject currentMembers ? currentMembers[name]?.DeepClone() : null;
                        }
                    }

                    break;
                case JsonArray items:
                    for (var index = 0; index < items.Count; index++)
                    {
                        var item = items[index];
                        if (item == null)
                        {
                            continue;
                        }

                        var currentItem = current is JsonArray currentItems && index < currentItems.Count ? currentItems[index] : null;
                        Resolve(memberAnnotations, item, currentItem);
                    }

                    break;
            }
        }

        private static bool IsSentinel(JsonNode? node)
        {
            return node is JsonValue value && value.TryGetValue(out string? text) && text == WriteOnlyConventions.RedactedSentinel;
        }
    }
}
