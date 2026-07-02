using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    [TestClass]
    public class SystemServiceStatePayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names + dictionary keys.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                        DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaName()
        {
            var schema = typeof(ServiceStatePayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "event must carry a [Schema] attribute");
            Assert.AreEqual("SystemServiceStatePayload", schema.Schema);
        }

        [TestMethod]
        public void RoundtripWithOptionalInformation()
        {
            var payload = new ServiceStatePayload("remote-vpn",
                                                  "0195f0d1-1111-7abc-8def-000000000001",
                                                  ServiceState.Started,
                                                  new Dictionary<string, string> { ["tunnelAddress"] = "100.64.0.7" });

            var roundtripped = JsonSerializer.Deserialize<ServiceStatePayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual("remote-vpn", roundtripped.ServiceName);
            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", roundtripped.InstanceId);
            Assert.AreEqual(ServiceState.Started, roundtripped.State);
            Assert.IsNotNull(roundtripped.Information);
            Assert.AreEqual("100.64.0.7", roundtripped.Information!["tunnelAddress"]);
        }

        [TestMethod]
        public void RoundtripWithoutInformation()
        {
            var payload = new ServiceStatePayload("remote-vpn", "s-1", ServiceState.Stopped);

            var roundtripped = JsonSerializer.Deserialize<ServiceStatePayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual(ServiceState.Stopped, roundtripped.State);
            Assert.IsNull(roundtripped.Information);
        }
    }
}
