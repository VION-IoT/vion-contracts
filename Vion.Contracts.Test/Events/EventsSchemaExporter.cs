using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization.Metadata;
using Vion.Contracts.Events;

namespace Vion.Contracts.Test.Events
{
    /// <summary>
    ///     Builds <c>Vion.Contracts/Events/events.schema.json</c> — the generator behind the committed export that
    ///     <see cref="EventsSchemaExport" /> ships. <see cref="EventsSchemaExportShould" /> compares a fresh build of it
    ///     against the committed file, and writes it instead when <see cref="WriteVariable" /> is set, which is what
    ///     <c>scripts/export-event-schemas.ps1</c> does.
    /// </summary>
    internal static class EventsSchemaExporter
    {
        /// <summary>Set to <c>1</c> to rewrite the committed export instead of comparing against it.</summary>
        public const string WriteVariable = "VION_CONTRACTS_WRITE_EVENT_SCHEMAS";

        private const string EventsNamespace = "Vion.Contracts.Events";

        private const string DefinitionTag = "x-vion-definition";

        private static readonly JsonSchemaExporterOptions TaggingOptions = new() { TransformSchemaNode = Tag };

        private static readonly JsonSerializerOptions FileOptions = new()
                                                                    {
                                                                        WriteIndented = true,
                                                                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                                                    };

        /// <summary>
        ///     Every top-level payload under <c>Events/*</c>: a public, non-nested type in a direction namespace whose name
        ///     ends in <c>Payload</c> — the naming rule CLAUDE.md sets for every payload.
        /// </summary>
        public static IReadOnlyList<Type> PayloadTypes { get; } = FindPayloadTypes();

        /// <summary>
        ///     The <c>schema</c> user-property value of a payload: its <see cref="SchemaAttribute" /> value, or the type
        ///     name where it has none (the <c>readonly record struct</c> payloads cannot carry the attribute).
        /// </summary>
        public static string SchemaName(Type payloadType)
        {
            return payloadType.GetCustomAttribute<SchemaAttribute>()?.Schema ?? payloadType.Name;
        }

        public static JsonTypeInfo TypeInfo(Type payloadType)
        {
            var typeInfo = EventsJsonContext.Default.GetTypeInfo(payloadType);

            return typeInfo ?? throw new InvalidOperationException($"{payloadType.FullName} is not registered.");
        }

        /// <summary>
        ///     The export's key for a type, in <c>payloads</c> and in <c>$defs</c> alike: its namespace relative to
        ///     <c>Vion.Contracts.Events</c>, sub-namespaces kept, plus its type name, declaring types first
        ///     (<c>CloudToMesh.Automations.UpsertAutomationPayload</c>,
        ///     <c>MeshToCloud.AlarmStateChangedPayload.AlarmParameter</c>). A type outside <c>Vion.Contracts.Events</c> is
        ///     keyed by its full namespace instead (<c>Microsoft.Extensions.Logging.LogLevel</c>), whose first segment is
        ///     never a direction name.
        /// </summary>
        public static string Key(Type type)
        {
            if (type.IsGenericType)
            {
                throw new InvalidOperationException($"No definition key rule for the generic type {type}.");
            }

            var names = new Stack<string>();
            for (var current = type; current is not null; current = current.DeclaringType)
            {
                names.Push(current.Name);
            }

            var name = string.Join(".", names);
            var space = type.Namespace ?? "";
            if (space == EventsNamespace)
            {
                return name;
            }

            if (space.StartsWith(EventsNamespace + ".", StringComparison.Ordinal))
            {
                return $"{space.Substring(EventsNamespace.Length + 1)}.{name}";
            }

            return space.Length == 0 ? name : $"{space}.{name}";
        }

        /// <summary>
        ///     The export: what <see cref="JsonSchemaExporter" /> writes for every payload over the context's options,
        ///     with every record, struct and enum a payload nests hoisted into one top-level <c>$defs</c> and referenced
        ///     as <c>#/$defs/&lt;key&gt;</c>, in place of the exporter's own pointer <c>$ref</c>s.
        /// </summary>
        public static JsonObject Build()
        {
            var definitions = new SortedDictionary<string, JsonObject>(StringComparer.Ordinal);
            var payloads = new JsonObject();
            foreach (var payloadType in PayloadTypes)
            {
                var exported = TypeInfo(payloadType).GetJsonSchemaAsNode(TaggingOptions);
                var hoister = new DefinitionHoister(exported, definitions);

                payloads[Key(payloadType)] = new JsonObject
                                             {
                                                 ["schema"] = SchemaName(payloadType),
                                                 ["jsonSchema"] = hoister.Hoist(),
                                             };
            }

            var defs = new JsonObject();
            foreach (var definition in definitions)
            {
                defs[definition.Key] = definition.Value;
            }

            return new JsonObject { ["$defs"] = defs, ["payloads"] = payloads };
        }

        /// <summary>The export as the committed file holds it: indented, LF line endings, one trailing newline.</summary>
        public static string BuildText()
        {
            return Build().ToJsonString(FileOptions).Replace("\r\n", "\n") + "\n";
        }

        /// <summary>The committed file's path in the working tree, found by walking up to the solution.</summary>
        public static string CommittedPath()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Vion.Contracts.sln")))
                {
                    return Path.Combine(directory.FullName, "Vion.Contracts", "Events", "events.schema.json");
                }
            }

            throw new InvalidOperationException($"No Vion.Contracts.sln above {AppContext.BaseDirectory}.");
        }

        // Marks every node of a named type with its definition key while the exporter writes it. Only a keyword is
        // added, so the exporter's pointer $refs still resolve against the tagged tree.
        private static JsonNode Tag(JsonSchemaExporterContext context, JsonNode schema)
        {
            if (context.Path.IsEmpty || schema is not JsonObject node)
            {
                return schema;
            }

            var type = Nullable.GetUnderlyingType(context.TypeInfo.Type) ?? context.TypeInfo.Type;
            if (IsNamed(type))
            {
                node[DefinitionTag] = Key(type);
            }

            return schema;
        }

        // Records, classes, structs and enums; never a collection, a dictionary, a primitive or a JsonNode.
        private static bool IsNamed(Type type)
        {
            if (type.IsEnum)
            {
                return true;
            }

            return EventsJsonContext.Default.GetTypeInfo(type)?.Kind == JsonTypeInfoKind.Object;
        }

        private static List<Type> FindPayloadTypes()
        {
            var exported = typeof(EventsJsonContext).Assembly.GetExportedTypes();
            var payloads = exported.Where(IsPayload);

            return payloads.OrderBy(Key, StringComparer.Ordinal).ToList();
        }

        private static bool IsPayload(Type type)
        {
            if (type.IsNested || !type.Name.EndsWith("Payload", StringComparison.Ordinal))
            {
                return false;
            }

            return type.Namespace?.StartsWith(EventsNamespace + ".", StringComparison.Ordinal) == true;
        }
    }
}
