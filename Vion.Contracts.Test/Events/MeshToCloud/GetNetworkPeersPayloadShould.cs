using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    [TestClass]
    public class GetNetworkPeersPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(GetNetworkPeersPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "get body must carry a [Schema] attribute");
            Assert.AreEqual("GetNetworkPeersPayload", schema.Schema);
        }

        [TestMethod]
        public void RoundTripAsAnEmptyBody()
        {
            var json = JsonSerializer.Serialize(new GetNetworkPeersPayload(), WireOptions);

            var roundtripped = JsonSerializer.Deserialize<GetNetworkPeersPayload>(json, WireOptions);

            Assert.AreEqual("{}", json);
            Assert.IsNotNull(roundtripped);
        }
    }
}
