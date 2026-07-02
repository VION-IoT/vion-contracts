using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Constants;
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
        public void RoundtripTheInstanceIdentifyingArgument()
        {
            var command = new StopSystemServicePayload([
                new ServiceArgument(RemoteAccessConstants.Arguments.SessionId, "0195f0d1-1111-7abc-8def-000000000001"),
            ]);

            var roundtripped = JsonSerializer.Deserialize<StopSystemServicePayload>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            var argument = roundtripped.Arguments.Single();
            Assert.AreEqual(RemoteAccessConstants.Arguments.SessionId, argument.Name);
            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", argument.Value);
        }

        [TestMethod]
        public void SerializeArgumentsAsACamelCaseNameValueList()
        {
            var command = new StopSystemServicePayload([
                new ServiceArgument(RemoteAccessConstants.Arguments.SessionId, "s-1"),
            ]);

            var json = JsonNode.Parse(JsonSerializer.Serialize(command, WireOptions))!;

            var arguments = json["arguments"]!.AsArray();
            Assert.HasCount(1, arguments);
            Assert.AreEqual("sessionId", arguments[0]!["name"]!.GetValue<string>());
            Assert.AreEqual("s-1", arguments[0]!["value"]!.GetValue<string>());
        }
    }
}
