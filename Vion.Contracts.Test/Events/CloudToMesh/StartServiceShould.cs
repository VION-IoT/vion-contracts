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
    public class StartServiceShould
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
            // The schema name is the wire contract cloud-api stamps and mesh validates on; a rename breaks dispatch.
            var schema = typeof(StartService).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StartService", schema.Schema);
        }

        [TestMethod]
        public void RoundtripItsNamedArguments()
        {
            var command = new StartService(new Dictionary<string, string>
                                           {
                                               [RemoteAccessConstants.Arguments.SessionId] = "0195f0d1-1111-7abc-8def-000000000001",
                                               [RemoteAccessConstants.Arguments.LoginServerUrl] = "https://abc.session.example",
                                               [RemoteAccessConstants.Arguments.EphemeralAuthKey] = "authkey-xyz",
                                               [RemoteAccessConstants.Arguments.ExpiresAtUtc] = "2026-07-02T10:00:00Z",
                                           });

            var roundtripped = JsonSerializer.Deserialize<StartService>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            Assert.HasCount(4, roundtripped.Arguments);
            Assert.AreEqual("https://abc.session.example", roundtripped.Arguments[RemoteAccessConstants.Arguments.LoginServerUrl]);
            Assert.AreEqual("authkey-xyz", roundtripped.Arguments[RemoteAccessConstants.Arguments.EphemeralAuthKey]);
            Assert.AreEqual("2026-07-02T10:00:00Z", roundtripped.Arguments[RemoteAccessConstants.Arguments.ExpiresAtUtc]);
        }

        [TestMethod]
        public void SerializeArgumentsAsACamelCaseKeyedObject()
        {
            var command = new StartService(new Dictionary<string, string>
                                           {
                                               [RemoteAccessConstants.Arguments.SessionId] = "s-1",
                                           });

            var json = JsonNode.Parse(JsonSerializer.Serialize(command, WireOptions))!;

            // `Arguments` serialises camelCase, and its keys stay the camelCase argument names.
            Assert.IsNotNull(json["arguments"], "arguments must serialise camelCase");
            Assert.AreEqual("s-1", json["arguments"]!["sessionId"]!.GetValue<string>());
        }
    }
}
