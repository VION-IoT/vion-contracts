using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    [TestClass]
    public class NetworkStatePayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(NetworkStatePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "state body must carry a [Schema] attribute");
            Assert.AreEqual("NetworkStatePayload", schema.Schema);
        }

        [TestMethod]
        public void RoundTripTheAddressesInOrderAndThePort()
        {
            var payload = new NetworkStatePayload(new List<string> { "gw-1.local", "192.168.1.10", "fe80::1" }, 8883);

            var roundtripped = JsonSerializer.Deserialize<NetworkStatePayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            CollectionAssert.AreEqual(payload.Addresses, roundtripped.Addresses);
            Assert.AreEqual(8883, roundtripped.Port);
        }

        [TestMethod]
        public void RoundTripAnEmptyAddressList()
        {
            var payload = new NetworkStatePayload(new List<string>(), 8883);

            var roundtripped = JsonSerializer.Deserialize<NetworkStatePayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.IsNotNull(roundtripped.Addresses);
            Assert.IsEmpty(roundtripped.Addresses);
        }
    }
}
