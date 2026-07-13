using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Conventions;
using Vion.Contracts.Introspection;

namespace Vion.Contracts.Test.Introspection
{
    [TestClass]
    public class LogicBlockIntrospectionResultShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void RejectAServicePropertyWithoutASchema()
        {
            // Schema is required on the dale->cloud introspection contract: a property carrying no schema
            // is malformed and rejected at deserialisation. Greenfield — introspection history is rebuilt,
            // so there are no pre-rich-types null-schema definitions to tolerate.
            const string noSchema = "{\"services\":[{\"identifier\":\"s\",\"properties\":[{\"identifier\":\"MaxCurrent\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LogicBlockIntrospectionResult>(noSchema, WireOptions));
        }

        [TestMethod]
        public void RejectAServiceMeasuringPointWithoutASchema()
        {
            const string noSchema = "{\"services\":[{\"identifier\":\"s\",\"measuringPoints\":[{\"identifier\":\"Power\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<LogicBlockIntrospectionResult>(noSchema, WireOptions));
        }

        [TestMethod]
        public void RoundTripAServiceInclusionGatePredicate()
        {
            // The definition view carries each gated service's [IncludedWhen] predicate as a typed field
            // (services have no annotation bag); a consumer resolves it per instance's parameter values.
            var result = new LogicBlockIntrospectionResult
                         {
                             TypeFullName = "Ns.ChargingStationEvtec",
                             Services =
                             {
                                 new LogicBlockIntrospectionResult.ServiceInfo
                                 { Identifier = "point2", IncludedWhen = "ChargePointCount >= 2" },
                                 new LogicBlockIntrospectionResult.ServiceInfo { Identifier = "point1" },
                             },
                         };

            var back = JsonSerializer.Deserialize<LogicBlockIntrospectionResult>(JsonSerializer.Serialize(result, WireOptions), WireOptions)!;

            Assert.AreEqual("ChargePointCount >= 2", back.Services.Single(s => s.Identifier == "point2").IncludedWhen);
            Assert.IsNull(back.Services.Single(s => s.Identifier == "point1").IncludedWhen, "an ungated service must carry no predicate");
        }

        [TestMethod]
        public void SerializeTheServiceInclusionGateAsCamelCaseIncludedWhen()
        {
            var result = new LogicBlockIntrospectionResult
                         {
                             TypeFullName = "Ns.ChargingStationEvtec",
                             Services =
                             {
                                 new LogicBlockIntrospectionResult.ServiceInfo
                                 { Identifier = "point2", IncludedWhen = "ChargePointCount >= 2" },
                             },
                         };

            var json = JsonNode.Parse(JsonSerializer.Serialize(result, WireOptions))!;

            Assert.AreEqual("ChargePointCount >= 2", json["services"]![0]!["includedWhen"]!.GetValue<string>());
        }

        [TestMethod]
        public void CarryInterfaceAndContractInclusionGatesUnderTheFrozenAnnotationKey()
        {
            // Binding-level gates ride the loose Annotations bag under the same key the change-detector pins —
            // the same mechanism as Multiplicity/Consumers. Producer and consumer use the C# const as the
            // vocabulary; the fixed key set is what makes the camelCase DictionaryKeyPolicy tolerable here
            // (contrast the parameter identifiers, which must survive verbatim and so travel as list values).
            var interfaceInfo = new LogicBlockIntrospectionResult.InterfaceInfo { Identifier = "point2" };
            interfaceInfo.Annotations[LogicBlockWiringConventions.IncludedWhenAnnotationKey] = "ChargePointCount >= 2";
            var contractInfo = new LogicBlockIntrospectionResult.ContractInfo { Identifier = "point2_do" };
            contractInfo.Annotations[LogicBlockWiringConventions.IncludedWhenAnnotationKey] = "ChargePointCount >= 2";

            Assert.AreEqual("ChargePointCount >= 2", interfaceInfo.Annotations[LogicBlockWiringConventions.IncludedWhenAnnotationKey]);
            Assert.AreEqual("ChargePointCount >= 2", contractInfo.Annotations[LogicBlockWiringConventions.IncludedWhenAnnotationKey]);
        }
    }
}
