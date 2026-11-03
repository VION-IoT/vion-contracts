using System;
using System.IO;

namespace Vion.Contracts.Events
{
    /// <summary>
    ///     The committed JSON Schema export of every <c>Events/*</c> payload, embedded in this assembly.
    ///     <para>
    ///         The export is one JSON object with two members, <c>$defs</c> and <c>payloads</c>. <c>payloads</c> maps a key
    ///         per payload to an entry holding <c>schema</c> and <c>jsonSchema</c>.
    ///     </para>
    ///     <para>
    ///         Both maps are keyed by one rule: the type's namespace relative to <c>Vion.Contracts.Events</c>,
    ///         sub-namespaces kept, plus its type name, declaring types first —
    ///         <c>CloudToMesh.Automations.UpsertAutomationPayload</c>,
    ///         <c>MeshToCloud.AlarmStateChangedPayload.AlarmParameter</c>. A type outside <c>Vion.Contracts.Events</c> is
    ///         keyed by its full name (<c>Microsoft.Extensions.Logging.LogLevel</c>), whose first segment is never a
    ///         direction. The namespace is in the key because two pairs of payloads share a type name across directions
    ///         with different shapes.
    ///     </para>
    ///     <para>
    ///         <c>schema</c> is the value the MQTT <c>schema</c>
    ///         user-property carries for that payload — the <see cref="SchemaAttribute" /> value where there is one,
    ///         otherwise the type name.
    ///         <c>jsonSchema</c> is what <c>System.Text.Json.Schema.JsonSchemaExporter</c> writes for the payload over
    ///         <see cref="EventsJsonContext" />'s options, with its nested types moved into <c>$defs</c>, so it describes the
    ///         bytes that context produces.
    ///     </para>
    ///     <para>
    ///         Every record, struct and enum a payload nests is defined once under <c>$defs</c> and referenced as
    ///         <c>#/$defs/&lt;key&gt;</c>, from a payload or from another definition; no other <c>$ref</c> occurs. A
    ///         definition never allows
    ///         <c>null</c>; a use that does is <c>{"anyOf": [{"$ref": …}, {"type": "null"}]}</c>. Collections and
    ///         dictionaries stay inline. A value typed <c>JsonNode</c> or <c>object</c> is the free-form schema
    ///         <c>true</c>.
    ///     </para>
    ///     <para>
    ///         The file is generated, committed as <c>Vion.Contracts/Events/events.schema.json</c>, and checked against a
    ///         fresh export by <c>Vion.Contracts.Test</c> on every test run; regenerate it with
    ///         <c>scripts/export-event-schemas.ps1</c>.
    ///     </para>
    /// </summary>
    public static class EventsSchemaExport
    {
        /// <summary>The manifest resource name the export is embedded under.</summary>
        public const string ResourceName = "Vion.Contracts.Events.events.schema.json";

        /// <summary>Opens the embedded export for reading. The caller disposes the stream.</summary>
        public static Stream Open()
        {
            return typeof(EventsSchemaExport).Assembly.GetManifestResourceStream(ResourceName) ??
                   throw new InvalidOperationException($"The embedded resource '{ResourceName}' is missing from {typeof(EventsSchemaExport).Assembly.GetName().Name}.");
        }
    }
}
