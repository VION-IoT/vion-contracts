using System.Collections.Immutable;
using System.Text.Json.Nodes;

namespace Vion.Contracts.TypeRef
{
    /// <summary>
    ///     Round-trips a <see cref="PropertyMetadata" /> document — the per-property triple of
    ///     data shape (<see cref="TypeSchema" />), UI hints (<see cref="Presentation" />), and
    ///     Dale-runtime flags (<see cref="RuntimeMetadata" />). Empty Presentation/Runtime are
    ///     emitted as <c>null</c> sibling values per spec §5.1.
    /// </summary>
    public static class PropertyMetadataSerialization
    {
        public static JsonNode ToJson(this PropertyMetadata metadata)
        {
            var obj = new JsonObject
                      {
                          ["schema"] = metadata.Schema.ToJsonSchema(),
                          ["presentation"] = metadata.Presentation.IsEmpty ? null : SerializePresentation(metadata.Presentation),
                          ["runtime"] = metadata.Runtime.IsEmpty ? null : SerializeRuntime(metadata.Runtime),
                      };
            return obj;
        }

        public static PropertyMetadata FromJson(JsonNode json)
        {
            if (json is not JsonObject obj)
            {
                throw new InvalidSchemaException("PropertyMetadata must be a JSON object");
            }

            var schemaNode = obj["schema"] ?? throw new InvalidSchemaException("PropertyMetadata missing 'schema'");
            var schema = TypeSchemaSerialization.FromJsonSchema(schemaNode);
            var presentation = obj["presentation"] is JsonObject pObj ? DeserializePresentation(pObj) : Presentation.None;
            var runtime = obj["runtime"] is JsonObject rObj ? DeserializeRuntime(rObj) : RuntimeMetadata.None;
            return new PropertyMetadata(schema, presentation, runtime);
        }

        private static JsonObject SerializePresentation(Presentation p)
        {
            var o = new JsonObject();
            if (p.DisplayName is not null)
            {
                o["displayName"] = p.DisplayName;
            }

            if (p.Group is not null)
            {
                o["group"] = p.Group;
            }

            if (p.Order is int order)
            {
                o["order"] = order;
            }

            if (p.Category is not null)
            {
                o["category"] = p.Category;
            }

            if (p.Importance is not null)
            {
                o["importance"] = p.Importance;
            }

            if (p.UIHint is not null)
            {
                o["uiHint"] = p.UIHint;
            }

            if (p.Decimals is int dec)
            {
                o["decimals"] = dec;
            }

            if (p.StatusMappings is { Count: > 0 } sm)
            {
                var smObj = new JsonObject();
                foreach (var kv in sm)
                {
                    smObj[kv.Key] = kv.Value;
                }

                o["statusMappings"] = smObj;
            }

            return o;
        }

        private static Presentation DeserializePresentation(JsonObject o)
        {
            return new Presentation
                   {
                       DisplayName = o["displayName"]?.GetValue<string>(),
                       Group = o["group"]?.GetValue<string>(),
                       Order = o["order"]?.GetValue<int>(),
                       Category = o["category"]?.GetValue<string>(),
                       Importance = o["importance"]?.GetValue<string>(),
                       UIHint = o["uiHint"]?.GetValue<string>(),
                       Decimals = o["decimals"]?.GetValue<int>(),
                       StatusMappings = o["statusMappings"] is JsonObject sm ? sm.ToImmutableDictionary(kv => kv.Key, kv => kv.Value!.GetValue<string>()) : null,
                   };
        }

        private static JsonObject SerializeRuntime(RuntimeMetadata r)
        {
            return new JsonObject { ["persistent"] = r.Persistent };
        }

        private static RuntimeMetadata DeserializeRuntime(JsonObject o)
        {
            return new RuntimeMetadata
                   {
                       Persistent = o["persistent"]?.GetValue<bool>() ?? false,
                   };
        }
    }
}