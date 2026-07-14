using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class SetLogicConfigurationPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys. The
        // DictionaryKeyPolicy is the whole reason instantiation-parameter identifiers travel as list-item
        // VALUES rather than dictionary KEYS (decision 0045) — this options object reproduces that hazard.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(SetLogicConfigurationPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("SetLogicConfigurationPayload", schema.Schema);
        }

        [TestMethod]
        public void CarryTheSharedLogicConfigurationBodyFlatOnTheWire()
        {
            // The shared body is inherited, not nested, so fields stay top-level — no "config" wrapper.
            var payload = MinimalPayload(new LogicConfiguration.LogicBlockInstance
                                         {
                                             Id = "b1",
                                             PackageId = "pkg",
                                             PackageVersion = "4.0.0",
                                             TypeFullName = "Ns.X",
                                             Name = "n",
                                             Services = [],
                                         });

            Assert.IsInstanceOfType<LogicConfiguration>(payload, "the payload must inherit the shared body");

            var json = JsonNode.Parse(JsonSerializer.Serialize(payload, WireOptions))!.AsObject();

            Assert.IsTrue(json.ContainsKey("logicBlockInstances"), "the shared body's fields stay top-level (flattened)");
            Assert.IsFalse(json.ContainsKey("config"), "the body is inherited, not nested under a 'config' wrapper");
        }

        [TestMethod]
        public void RoundTripInstantiationParameterValuesAsAList()
        {
            var payload = MinimalPayload(new LogicConfiguration.LogicBlockInstance
                                         {
                                             Id = "b1",
                                             PackageId = "pkg",
                                             PackageVersion = "3.7.0",
                                             TypeFullName = "Ns.ChargingStationEvtec",
                                             Name = "Station",
                                             Services = [],
                                             InstantiationParameterValues =
                                             [
                                                 new LogicConfiguration.InstantiationParameterValue
                                                 { Identifier = "ChargePointCount", Value = JsonValue.Create(3) },
                                                 new LogicConfiguration.InstantiationParameterValue
                                                 { Identifier = "Model", Value = JsonValue.Create("Cappuccino") },
                                             ],
                                         });

            var roundtripped = JsonSerializer.Deserialize<SetLogicConfigurationPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            var values = roundtripped.LogicBlockInstances.Single().InstantiationParameterValues!;
            Assert.HasCount(2, values);
            Assert.AreEqual(3, values.Single(v => v.Identifier == "ChargePointCount").Value!.GetValue<int>());
            Assert.AreEqual("Cappuccino", values.Single(v => v.Identifier == "Model").Value!.GetValue<string>());
        }

        [TestMethod]
        public void PreservePascalCaseIdentifiersThroughTheCamelCaseKeyPolicy()
        {
            // The reason for the list shape: the identifier sits in a VALUE position, so the camelCase
            // DictionaryKeyPolicy never touches it. "ChargePointCount" must survive verbatim.
            var payload = MinimalPayload(new LogicConfiguration.LogicBlockInstance
                                         {
                                             Id = "b1",
                                             PackageId = "pkg",
                                             PackageVersion = "3.7.0",
                                             TypeFullName = "Ns.ChargingStationEvtec",
                                             Name = "Station",
                                             Services = [],
                                             InstantiationParameterValues =
                                             [
                                                 new LogicConfiguration.InstantiationParameterValue
                                                 { Identifier = "ChargePointCount", Value = JsonValue.Create(3) },
                                             ],
                                         });

            var json = JsonNode.Parse(JsonSerializer.Serialize(payload, WireOptions))!;

            var item = json["logicBlockInstances"]![0]!["instantiationParameterValues"]![0]!;
            Assert.AreEqual("ChargePointCount", item["identifier"]!.GetValue<string>(), "the PascalCase identifier must survive the camelCase key policy intact");
            Assert.AreEqual(3, item["value"]!.GetValue<int>());
        }

        [TestMethod]
        public void TreatInstantiationParameterValuesAsAdditiveAndOptional()
        {
            // A payload from before the feature (no instantiationParameterValues) still deserializes; the
            // field is null, distinguishable from an explicit empty list.
            const string legacy =
                "{\"logicBlockInstances\":[{\"id\":\"b1\",\"packageId\":\"pkg\",\"packageVersion\":\"3.6.0\",\"typeFullName\":\"Ns.X\",\"name\":\"n\",\"services\":[]}]," +
                "\"interfaceMappings\":[],\"contractMappings\":[]}";

            var payload = JsonSerializer.Deserialize<SetLogicConfigurationPayload>(legacy, WireOptions)!;

            Assert.IsNull(payload.LogicBlockInstances.Single().InstantiationParameterValues);
        }

        private static SetLogicConfigurationPayload MinimalPayload(LogicConfiguration.LogicBlockInstance instance)
        {
            return new SetLogicConfigurationPayload
                   {
                       LogicBlockInstances = [instance],
                       InterfaceMappings = [],
                       ContractMappings = [],
                   };
        }
    }
}
