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
    public class StartSystemServicePayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            // The schema name is the wire contract cloud-api stamps and mesh validates on; a rename breaks dispatch.
            var schema = typeof(StartSystemServicePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StartSystemServicePayload", schema.Schema);
        }

        [TestMethod]
        public void RoundtripItsInstanceIdAndNamedArguments()
        {
            var command = new StartSystemServicePayload("0195f0d1-1111-7abc-8def-000000000001",
            [
                new ServiceArgument(RemoteAccessConstants.Arguments.LoginServerUrl, "https://abc.session.example"),
                new ServiceArgument(RemoteAccessConstants.Arguments.EphemeralAuthKey, "authkey-xyz"),
                new ServiceArgument(RemoteAccessConstants.Arguments.ExpiresAtUtc, "2026-07-02T10:00:00Z"),
            ]);

            var roundtripped = JsonSerializer.Deserialize<StartSystemServicePayload>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", roundtripped.InstanceId);
            Assert.HasCount(3, roundtripped.Arguments);
            var byName = roundtripped.Arguments.ToDictionary(argument => argument.Name, argument => argument.Value);
            Assert.AreEqual("https://abc.session.example", byName[RemoteAccessConstants.Arguments.LoginServerUrl]);
            Assert.AreEqual("authkey-xyz", byName[RemoteAccessConstants.Arguments.EphemeralAuthKey]);
            Assert.AreEqual("2026-07-02T10:00:00Z", byName[RemoteAccessConstants.Arguments.ExpiresAtUtc]);
        }

        [TestMethod]
        public void SerializeInstanceIdTopLevelAndArgumentsAsACamelCaseNameValueList()
        {
            var command = new StartSystemServicePayload("s-1",
            [
                new ServiceArgument(RemoteAccessConstants.Arguments.LoginServerUrl, "https://abc.session.example"),
            ]);

            var json = JsonNode.Parse(JsonSerializer.Serialize(command, WireOptions))!;

            Assert.AreEqual("s-1", json["instanceId"]!.GetValue<string>());
            var arguments = json["arguments"]!.AsArray();
            Assert.HasCount(1, arguments);
            Assert.AreEqual("loginServerUrl", arguments[0]!["name"]!.GetValue<string>());
            Assert.AreEqual("https://abc.session.example", arguments[0]!["value"]!.GetValue<string>());
        }
    }
}
