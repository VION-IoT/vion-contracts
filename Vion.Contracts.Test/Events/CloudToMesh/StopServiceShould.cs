using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Constants;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class StopServiceShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(StopService).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StopService", schema.Schema);
        }

        [TestMethod]
        public void RoundtripTheInstanceIdentifyingArgument()
        {
            var command = new StopService(new Dictionary<string, string>
                                          {
                                              [RemoteAccessConstants.Arguments.SessionId] = "0195f0d1-1111-7abc-8def-000000000001",
                                          });

            var roundtripped = JsonSerializer.Deserialize<StopService>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", roundtripped.Arguments[RemoteAccessConstants.Arguments.SessionId]);
        }

        [TestMethod]
        public void SerializeArgumentsAsACamelCaseKeyedObject()
        {
            var command = new StopService(new Dictionary<string, string>
                                          {
                                              [RemoteAccessConstants.Arguments.SessionId] = "s-1",
                                          });

            var json = JsonNode.Parse(JsonSerializer.Serialize(command, WireOptions))!;

            Assert.IsNotNull(json["arguments"], "arguments must serialise camelCase");
            Assert.AreEqual("s-1", json["arguments"]!["sessionId"]!.GetValue<string>());
        }
    }
}
