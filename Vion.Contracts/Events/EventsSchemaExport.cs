using System;
using System.IO;

namespace Vion.Contracts.Events
{
    /// <summary>
    ///     The committed JSON Schema export of every <c>Events/*</c> payload, embedded in this assembly.
    ///     <para>
    ///         The export is one JSON object whose <c>payloads</c> member maps a key per payload to an entry holding two
    ///         members, <c>schema</c> and <c>jsonSchema</c>. The key is <c>&lt;Direction&gt;.&lt;TypeName&gt;</c> — the
    ///         namespace segment under <c>Vion.Contracts.Events</c> and the type name, because two pairs of payloads share a
    ///         type name across directions with different shapes. <c>schema</c> is the value the MQTT <c>schema</c>
    ///         user-property carries for that payload — the <see cref="SchemaAttribute" /> value where there is one,
    ///         otherwise the type name.
    ///         <c>jsonSchema</c> is what <c>System.Text.Json.Schema.JsonSchemaExporter</c> writes for the payload over
    ///         <see cref="EventsJsonContext" />'s options, so it describes the bytes that context produces.
    ///     </para>
    ///     <para>
    ///         Each <c>jsonSchema</c> is a standalone schema: the exporter writes a type met a second time — a recursive
    ///         one, or one used by two properties — as a <c>$ref</c> JSON Pointer relative to that <c>jsonSchema</c>, not to
    ///         the file. A consumer that moves a schema into a larger document rewrites those pointers. A value typed
    ///         <c>JsonNode</c> or <c>object</c> is the free-form schema <c>true</c>.
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
