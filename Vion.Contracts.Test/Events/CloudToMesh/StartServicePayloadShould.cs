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
    public class StartServicePayloadShould
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
            var schema = typeof(StartServicePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StartService", schema.Schema);
        }

        [TestMethod]
        public void RoundtripItsNamedArguments()
        {
            var command = new StartServicePayload([
                new ServiceArgument(RemoteAccessConstants.Arguments.SessionId, "0195f0d1-1111-7abc-8def-000000000001"),
                new ServiceArgument(RemoteAccessConstants.Arguments.LoginServerUrl, "https://abc.session.example"),
                new ServiceArgument(RemoteAccessConstants.Arguments.EphemeralAuthKey, "authkey-xyz"),
                new ServiceArgument(RemoteAccessConstants.Arguments.ExpiresAtUtc, "2026-07-02T10:00:00Z"),
            ]);

            var roundtripped = JsonSerializer.Deserialize<StartServicePayload>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            Assert.HasCount(4, roundtripped.Arguments);
            var byName = roundtripped.Arguments.ToDictionary(a => a.Name, a => a.Value);
            Assert.AreEqual("https://abc.session.example", byName[RemoteAccessConstants.Arguments.LoginServerUrl]);
            Assert.AreEqual("authkey-xyz", byName[RemoteAccessConstants.Arguments.EphemeralAuthKey]);
            Assert.AreEqual("2026-07-02T10:00:00Z", byName[RemoteAccessConstants.Arguments.ExpiresAtUtc]);
        }

        [TestMethod]
        public void SerializeArgumentsAsACamelCaseNameValueList()
        {
            var command = new StartServicePayload([
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
