using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class StopSystemServicePayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(StopSystemServicePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StopSystemServicePayload", schema.Schema);
        }

        [TestMethod]
        public void RoundtripItsInstanceId()
        {
            var command = new StopSystemServicePayload("0195f0d1-1111-7abc-8def-000000000001");

            var roundtripped = JsonSerializer.Deserialize<StopSystemServicePayload>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", roundtripped.InstanceId);
        }

        [TestMethod]
        public void SerializeInstanceIdAsACamelCaseTopLevelField()
        {
            var command = new StopSystemServicePayload("s-1");

            var json = JsonNode.Parse(JsonSerializer.Serialize(command, WireOptions))!;

            Assert.AreEqual("s-1", json["instanceId"]!.GetValue<string>());
        }
    }
}
