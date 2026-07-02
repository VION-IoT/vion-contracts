using System;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class RemoteConsoleCommandPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            // The schema name is the wire contract cloud-api stamps and mesh validates on; a rename breaks dispatch.
            var schema = typeof(RemoteConsoleCommandPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "payload must carry a [Schema] attribute");
            Assert.AreEqual("RemoteConsoleCommandPayload", schema.Schema);
        }

        [TestMethod]
        public void RoundtripAStartCommandWithItsSessionParameters()
        {
            var sessionId = Guid.NewGuid();
            var expiresAt = new DateTimeOffset(2026,
                                               7,
                                               2,
                                               10,
                                               0,
                                               0,
                                               TimeSpan.Zero);
            var payload = new RemoteConsoleCommandPayload(sessionId,
                                                          RemoteAccessAction.Start,
                                                          new RemoteAccessSessionParameters("https://abc.session.example", "authkey-xyz", expiresAt));

            var roundtripped = JsonSerializer.Deserialize<RemoteConsoleCommandPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual(sessionId, roundtripped.SessionId);
            Assert.AreEqual(RemoteAccessAction.Start, roundtripped.Action);
            Assert.IsNotNull(roundtripped.Parameters);
            Assert.AreEqual("https://abc.session.example", roundtripped.Parameters!.LoginServerUrl);
            Assert.AreEqual("authkey-xyz", roundtripped.Parameters.EphemeralAuthKey);
            Assert.AreEqual(expiresAt, roundtripped.Parameters.ExpiresAtUtc);
        }

        [TestMethod]
        public void RoundtripAStopCommandWithoutParameters()
        {
            var sessionId = Guid.NewGuid();
            var payload = new RemoteConsoleCommandPayload(sessionId, RemoteAccessAction.Stop, null);

            var roundtripped = JsonSerializer.Deserialize<RemoteConsoleCommandPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual(sessionId, roundtripped.SessionId);
            Assert.AreEqual(RemoteAccessAction.Stop, roundtripped.Action);
            Assert.IsNull(roundtripped.Parameters);
        }

        [TestMethod]
        public void SerializeItsFieldsWithCamelCaseWireNames()
        {
            var payload = new RemoteConsoleCommandPayload(Guid.NewGuid(),
                                                          RemoteAccessAction.Start,
                                                          new RemoteAccessSessionParameters("https://abc.session.example", "authkey-xyz", DateTimeOffset.UnixEpoch));

            var json = JsonNode.Parse(JsonSerializer.Serialize(payload, WireOptions))!;

            Assert.IsNotNull(json["sessionId"], "sessionId must serialise camelCase");
            Assert.IsNotNull(json["parameters"]!["loginServerUrl"], "loginServerUrl must serialise camelCase");
            Assert.IsNotNull(json["parameters"]!["ephemeralAuthKey"]);
            Assert.IsNotNull(json["parameters"]!["expiresAtUtc"]);
        }
    }
}
