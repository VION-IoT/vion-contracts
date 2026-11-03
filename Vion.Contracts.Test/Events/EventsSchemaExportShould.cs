using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

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

        private static JsonObject Payloads()
        {
            return EventsSchemaExporter.Build()["payloads"]!.AsObject();
        }
    }
}
