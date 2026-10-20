using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class NetworkPeersStatePayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(NetworkPeersStatePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "state body must carry a [Schema] attribute");
            Assert.AreEqual("NetworkPeersStatePayload", schema.Schema);
        }

        [TestMethod]
        public void RoundTripEveryPeer()
        {
            var payload = new NetworkPeersStatePayload(new List<NetworkPeer>
                                                       {
                                                           new(Guid.NewGuid(), "tenant/installation-b", new List<string> { "gw-b.local", "10.0.0.2" }, 8883),
                                                           new(Guid.NewGuid(), "tenant/installation-c", new List<string>(), 8884),
                                                       });

            var roundtripped = JsonSerializer.Deserialize<NetworkPeersStatePayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.HasCount(payload.Peers.Count, roundtripped.Peers);
            for (var i = 0; i < payload.Peers.Count; i++)
            {
                Assert.AreEqual(payload.Peers[i].GatewayId, roundtripped.Peers[i].GatewayId);
                Assert.AreEqual(payload.Peers[i].InstallationTopic, roundtripped.Peers[i].InstallationTopic);
                CollectionAssert.AreEqual(payload.Peers[i].Addresses, roundtripped.Peers[i].Addresses);
                Assert.AreEqual(payload.Peers[i].Port, roundtripped.Peers[i].Port);
            }
        }
    }
}
