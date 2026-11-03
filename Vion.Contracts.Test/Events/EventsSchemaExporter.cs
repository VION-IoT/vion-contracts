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

        private static readonly JsonSerializerOptions FileOptions = new()
                                                                    {
                                                                        WriteIndented = true,
                                                                        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                                                                    };

        /// <summary>
        ///     Every top-level payload under <c>Events/*</c>: a public, non-nested type in a direction namespace whose name
        ///     ends in <c>Payload</c> — the naming rule CLAUDE.md sets for every payload.
        /// </summary>
        public static IReadOnlyList<Type> PayloadTypes { get; } = typeof(EventsJsonContext).Assembly
                                                                                           .GetExportedTypes()
                                                                                           .Where(type => !type.IsNested &&
                                                                                                          type.Namespace?.StartsWith(EventsNamespace + ".",
                                                                                                                                     StringComparison.Ordinal) == true &&
                                                                                                          type.Name.EndsWith("Payload", StringComparison.Ordinal))
                                                                                           .OrderBy(Key, StringComparer.Ordinal)
                                                                                           .ToList();

        /// <summary>The export's key for a payload: <c>&lt;Direction&gt;.&lt;TypeName&gt;</c>.</summary>
        public static string Key(Type payloadType)
        {
            var direction = payloadType.Namespace!.Substring(EventsNamespace.Length + 1).Split('.')[0];

            return $"{direction}.{payloadType.Name}";
        }

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
            return EventsJsonContext.Default.GetTypeInfo(payloadType) ??
                   throw new InvalidOperationException($"{payloadType.FullName} is not registered in {nameof(EventsJsonContext)}.");
        }

        public static JsonObject Build()
        {
            var payloads = new JsonObject();
            foreach (var payloadType in PayloadTypes)
            {
                payloads[Key(payloadType)] = new JsonObject
                                             {
                                                 ["schema"] = SchemaName(payloadType),
                                                 ["jsonSchema"] = TypeInfo(payloadType).GetJsonSchemaAsNode(),
                                             };
            }

            return new JsonObject { ["payloads"] = payloads };
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
    }
}
