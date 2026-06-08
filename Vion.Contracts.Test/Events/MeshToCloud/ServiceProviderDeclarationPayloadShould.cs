using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    [TestClass]
    public class ServiceProviderDeclarationPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryPropertySchemaAsAStructuredObjectNotADoubleEncodedString()
        {
            var payload = new ServiceProviderDeclarationPayload
                          {
                              Services =
                              [
                                  new ServiceProviderDeclarationPayload.ServiceInfo
                                  {
                                      Identifier = "client-config",
                                      Properties =
                                      [
                                          new ServiceProviderDeclarationPayload.PropertyInfo
                                          {
                                              Identifier = "Port",
                                              Schema = new JsonObject { ["type"] = "integer", ["format"] = "int32" },
                                          },
                                      ],
                                  },
                              ],
                          };

            var json = JsonNode.Parse(JsonSerializer.Serialize(payload, WireOptions))!;
            var property = json["services"]![0]!["properties"]![0]!;

            Assert.IsTrue(property["schema"] is JsonObject, "schema must serialise as a structured object, not a string");
            Assert.AreEqual("integer", property["schema"]!["type"]!.GetValue<string>());
            Assert.AreEqual("int32", property["schema"]!["format"]!.GetValue<string>());
        }

        [TestMethod]
        public void RoundtripTheSchemaPresentationRuntimeTripleForPropertiesAndMeasuringPoints()
        {
            var payload = new ServiceProviderDeclarationPayload
                          {
                              Services =
                              [
                                  new ServiceProviderDeclarationPayload.ServiceInfo
                                  {
                                      Identifier = "client-config",
                                      Properties =
                                      [
                                          new ServiceProviderDeclarationPayload.PropertyInfo
                                          {
                                              Identifier = "Password",
                                              Schema = JsonNode.Parse("{\"type\":[\"string\",\"null\"],\"writeOnly\":true}")!,
                                              Presentation = new JsonObject { ["group"] = "Connection" },
                                              Runtime = new JsonObject { ["persistent"] = true },
                                          },
                                      ],
                                      MeasuringPoints =
                                      [
                                          new ServiceProviderDeclarationPayload.MeasuringPointInfo
                                          {
                                              Identifier = "Throughput",
                                              Schema = new JsonObject { ["type"] = "number", ["format"] = "double" },
                                          },
                                      ],
                                  },
                              ],
                          };

            var roundtripped = JsonSerializer.Deserialize<ServiceProviderDeclarationPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            var property = roundtripped.Services[0].Properties![0];
            Assert.AreEqual("Password", property.Identifier);
            Assert.IsTrue(JsonNode.DeepEquals(JsonNode.Parse("{\"type\":[\"string\",\"null\"],\"writeOnly\":true}"), property.Schema));
            Assert.AreEqual("Connection", property.Presentation!["group"]!.GetValue<string>());
            Assert.IsTrue(property.Runtime!["persistent"]!.GetValue<bool>());

            var measuringPoint = roundtripped.Services[0].MeasuringPoints![0];
            Assert.AreEqual("Throughput", measuringPoint.Identifier);
            Assert.AreEqual("double", measuringPoint.Schema["format"]!.GetValue<string>());
        }

        [TestMethod]
        public void RejectADeclarationWhosePropertyCarriesNoSchema()
        {
            // Schema is required. With no production declaration data there are no pre-migration messages
            // to tolerate, so a property without a `schema` sibling is malformed and is rejected at
            // deserialisation rather than persisted schema-less.
            const string noSchema = "{\"services\":[{\"identifier\":\"client-config\",\"properties\":[{\"identifier\":\"Port\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ServiceProviderDeclarationPayload>(noSchema, WireOptions));
        }

        [TestMethod]
        public void RejectADeclarationWhoseMeasuringPointCarriesNoSchema()
        {
            const string noSchema = "{\"services\":[{\"identifier\":\"client-config\",\"measuringPoints\":[{\"identifier\":\"Throughput\"}]}]}";

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ServiceProviderDeclarationPayload>(noSchema, WireOptions));
        }
    }
}
