using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    /// <summary>
    ///     Pins the exact wire shape of <see cref="PropertiesStatePayload" /> through <see cref="EventsJsonContext" />. It is
    ///     the one <c>Events/*</c> payload that reaches a client — mesh publishes it, and it travels on to the dashboard
    ///     on the property-state channel — so its bytes are a contract beyond the platform, and the
    ///     <see cref="PropertyState.Value" /> it carries can be any JSON value.
    /// </summary>
    [TestClass]
    public class PropertiesStatePayloadShould
    {
        // The reflection-based options mesh's PropertyJsonContext is equivalent to; the context must agree with them.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void WriteAnObjectValueVerbatim()
        {
            // The value's own keys are not touched by the camelCase policies: a JsonNode is written as it is.
            AssertExact(new PropertiesStatePayload([new PropertyState("Setpoint", JsonNode.Parse("{\"Min\":5,\"max\":30.5,\"unit\":\"K\"}"))]),
                        "{\"propertiesState\":[{\"propertyIdentifier\":\"Setpoint\",\"value\":{\"Min\":5,\"max\":30.5,\"unit\":\"K\"}}]}");
        }

        [TestMethod]
        public void WriteANumberValue()
        {
            AssertExact(new PropertiesStatePayload([new PropertyState("Power", JsonValue.Create(11.25))]),
                        "{\"propertiesState\":[{\"propertyIdentifier\":\"Power\",\"value\":11.25}]}");
            AssertExact(new PropertiesStatePayload([new PropertyState("Count", JsonValue.Create(3))]), "{\"propertiesState\":[{\"propertyIdentifier\":\"Count\",\"value\":3}]}");
        }

        [TestMethod]
        public void WriteAStringValue()
        {
            AssertExact(new PropertiesStatePayload([new PropertyState("Mode", JsonValue.Create("Eco"))]),
                        "{\"propertiesState\":[{\"propertyIdentifier\":\"Mode\",\"value\":\"Eco\"}]}");
        }

        [TestMethod]
        public void WriteANullValueAsAnExplicitNull()
        {
            AssertExact(new PropertiesStatePayload([new PropertyState("Fault", null)]), "{\"propertiesState\":[{\"propertyIdentifier\":\"Fault\",\"value\":null}]}");
        }

        [TestMethod]
        public void KeepThePropertyIdentifierCaseAndTheOrderOfStates()
        {
            AssertExact(new PropertiesStatePayload([new PropertyState("IsEnabled", JsonValue.Create(true)), new PropertyState("Port", JsonValue.Create(502))]),
                        "{\"propertiesState\":[{\"propertyIdentifier\":\"IsEnabled\",\"value\":true},{\"propertyIdentifier\":\"Port\",\"value\":502}]}");
        }

        [TestMethod]
        public void WriteAnEmptyBatch()
        {
            AssertExact(new PropertiesStatePayload([]), "{\"propertiesState\":[]}");
        }

        [TestMethod]
        public void ExportTheValueAsFreeForm()
        {
            // JsonSchemaExporter writes the any-schema `true` for a JsonNode; cloud-api layers x-vion-valueSchema on it.
            var export = EventsSchemaExporter.Build();
            var items = export["payloads"]!["MeshToCloud.PropertiesStatePayload"]!["jsonSchema"]!["properties"]!["propertiesState"]!["items"]!;
            var value = export["$defs"]!["MeshToCloud.PropertyState"]!["properties"]!["value"]!;

            Assert.AreEqual("#/$defs/MeshToCloud.PropertyState", (string)items["$ref"]!);
            Assert.AreEqual(JsonValueKind.True, value.GetValueKind());
        }

        private static void AssertExact(PropertiesStatePayload payload, string expectedJson)
        {
            var typeInfo = EventsJsonContext.Default.PropertiesStatePayload;
            var throughContext = JsonSerializer.Serialize(payload, typeInfo);

            Assert.AreEqual(expectedJson, throughContext, "through EventsJsonContext");
            Assert.AreEqual(JsonSerializer.Serialize(payload, WireOptions), throughContext, "context and reflection disagree");

            var roundTripped = JsonSerializer.Deserialize(expectedJson, typeInfo);

            Assert.AreEqual(expectedJson, JsonSerializer.Serialize(roundTripped, typeInfo), "round-trip");
            CollectionAssert.AreEqual(payload.PropertiesState.Select(state => state.PropertyIdentifier).ToList(),
                                      roundTripped.PropertiesState.Select(state => state.PropertyIdentifier).ToList());
        }
    }
}
