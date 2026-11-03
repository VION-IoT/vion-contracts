using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;
using Vion.Contracts.Hw.Di;

namespace Vion.Contracts.Test.Events
{
    /// <summary>
    ///     The schema-diff gate for <c>Events/*</c>: the committed <c>events.schema.json</c> must be exactly what
    ///     <see cref="EventsSchemaExporter" /> builds from <see cref="EventsJsonContext" /> right now. A payload whose
    ///     shape changed, a payload added without registering it, or an enum converter dropped from the context all
    ///     change the export, so they fail here — in the <c>dotnet test</c> the CI gate runs on every pull request —
    ///     until the export is regenerated with <c>scripts/export-event-schemas.ps1</c> and the diff is reviewed.
    /// </summary>
    [TestClass]
    public class EventsSchemaExportShould
    {
        private static readonly Regex DefinitionReference = new(@"^#/\$defs/[^/]+$");

        [TestMethod]
        public void MatchAFreshExportOfTheContext()
        {
            var fresh = EventsSchemaExporter.BuildText();

            if (Environment.GetEnvironmentVariable(EventsSchemaExporter.WriteVariable) == "1")
            {
                File.WriteAllText(EventsSchemaExporter.CommittedPath(), fresh);

                return;
            }

            // Read through the shipped accessor, so the test also proves the export reaches the package.
            using var reader = new StreamReader(EventsSchemaExport.Open());
            var committed = reader.ReadToEnd().Replace("\r\n", "\n");

            Assert.AreEqual(fresh,
                            committed,
                            "events.schema.json is stale: run `pwsh scripts/export-event-schemas.ps1` and review the diff — a changed shape is a version question, see CLAUDE.md § Backwards compatibility.");
        }

        [TestMethod]
        public void RegisterEveryPayloadInTheContext()
        {
            var unregistered = EventsSchemaExporter.PayloadTypes.Where(type => EventsJsonContext.Default.GetTypeInfo(type) is null).Select(type => type.FullName).ToList();

            Assert.IsEmpty(unregistered, $"Register these in {nameof(EventsJsonContext)}: {string.Join(", ", unregistered)}");
        }

        [TestMethod]
        public void KeyBothPayloadsOfASharedTypeNameByDirection()
        {
            var payloads = Payloads();

            // Same type name, different shape, published under the same schema string: only the direction tells them apart.
            Assert.AreEqual("ServiceProviderRegistrationAcceptedPayload", (string)payloads["CloudToMesh.ServiceProviderRegistrationAcceptedPayload"]!["schema"]!);
            Assert.AreEqual("ServiceProviderRegistrationAcceptedPayload", (string)payloads["MeshToServiceProvider.ServiceProviderRegistrationAcceptedPayload"]!["schema"]!);
            Assert.AreEqual("ServiceProviderRegistrationDeniedPayload", (string)payloads["CloudToMesh.ServiceProviderRegistrationDeniedPayload"]!["schema"]!);
            Assert.AreEqual("ServiceProviderRegistrationDeniedPayload", (string)payloads["MeshToServiceProvider.ServiceProviderRegistrationDeniedPayload"]!["schema"]!);
        }

        [TestMethod]
        public void KeyAPayloadInANestedNamespaceByItsDirection()
        {
            Assert.IsTrue(Payloads().ContainsKey("CloudToMesh.UpsertAutomationPayload"));
        }

        [TestMethod]
        public void NameARecordStructPayloadAfterItsType()
        {
            // SchemaAttribute targets classes and interfaces only, so the record-struct payloads take nameof(T) — the
            // string mesh's PropertyStatePublisher publishes PropertiesStatePayload under.
            Assert.AreEqual(nameof(PropertiesStatePayload), (string)Payloads()["MeshToCloud.PropertiesStatePayload"]!["schema"]!);
        }

        [TestMethod]
        public void UseTheSchemaAttributeValueWhereThereIsOne()
        {
            foreach (var payloadType in EventsSchemaExporter.PayloadTypes)
            {
                // CLAUDE.md: the [Schema] value must equal the type name — mesh validates inbound messages by nameof(T).
                Assert.AreEqual(payloadType.Name, EventsSchemaExporter.SchemaName(payloadType), payloadType.FullName);
            }
        }

        [TestMethod]
        public void ReferenceOnlyNamedDefinitionsThatExist()
        {
            // A pointer $ref names a position, which moves with the layout and breaks once a schema is placed elsewhere;
            // a #/$defs/<key> names the type. Every reference must be the second kind, and resolve.
            var export = EventsSchemaExporter.Build();
            var definitions = export["$defs"]!.AsObject();
            var references = References(export).ToList();

            Assert.IsNotEmpty(references);
            foreach (var reference in references)
            {
                Assert.IsTrue(DefinitionReference.IsMatch(reference), $"Not a named definition: {reference}");
                Assert.IsTrue(definitions.ContainsKey(reference.Substring("#/$defs/".Length)), $"Does not resolve: {reference}");
            }
        }

        [TestMethod]
        public void ExpressARecursiveTypeAsAReferenceToItsOwnDefinition()
        {
            var component = EventsSchemaExporter.Build()["$defs"]!["MeshToCloud.Component"]!;

            Assert.AreEqual("#/$defs/MeshToCloud.Component", (string)component["properties"]!["subComponents"]!["items"]!["$ref"]!);
        }

        [TestMethod]
        public void KeyADefinitionByItsNamespaceUnderEventsAndItsDeclaringTypes()
        {
            Assert.AreEqual("MeshToCloud.PropertyState", EventsSchemaExporter.DefinitionKey(typeof(PropertyState)));
            Assert.AreEqual("MeshToCloud.AlarmStateChangedPayload.AlarmParameter", EventsSchemaExporter.DefinitionKey(typeof(AlarmStateChangedPayload.AlarmParameter)));
        }

        [TestMethod]
        public void KeyADefinitionOutsideEventsByAFullNameThatIsNoDirection()
        {
            var directions = EventsSchemaExporter.PayloadTypes.Select(type => EventsSchemaExporter.Key(type).Split('.')[0]).ToHashSet();
            var outside = new[] { typeof(LogLevel), typeof(DiStatePayload) };

            Assert.AreEqual("Microsoft.Extensions.Logging.LogLevel", EventsSchemaExporter.DefinitionKey(typeof(LogLevel)));
            Assert.AreEqual("Vion.Contracts.Hw.Di.DiStatePayload", EventsSchemaExporter.DefinitionKey(typeof(DiStatePayload)));
            foreach (var type in outside)
            {
                Assert.DoesNotContain(EventsSchemaExporter.DefinitionKey(type).Split('.')[0], directions, type.FullName);
            }
        }

        private static IEnumerable<string> References(JsonNode? node)
        {
            switch (node)
            {
                case JsonObject schema:
                    foreach (var member in schema)
                    {
                        if (member.Key == "$ref")
                        {
                            yield return (string)member.Value!;
                        }

                        foreach (var reference in References(member.Value))
                        {
                            yield return reference;
                        }
                    }

                    break;
                case JsonArray array:
                    foreach (var reference in array.SelectMany(References))
                    {
                        yield return reference;
                    }

                    break;
            }
        }

        private static JsonObject Payloads()
        {
            return EventsSchemaExporter.Build()["payloads"]!.AsObject();
        }
    }
}
