using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.ServiceProviderToMesh;

namespace Vion.Contracts.Test.Events.ServiceProviderToMesh
{
    [TestClass]
    public class LogicConfigurationLoadFailedPayloadShould
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
            var schema = typeof(LogicConfigurationLoadFailedPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "event must carry a [Schema] attribute");
            Assert.AreEqual("LogicConfigurationLoadFailedPayload", schema.Schema);
        }

        [TestMethod]
        public void RoundtripTheFailureReason()
        {
            var payload = new LogicConfigurationLoadFailedPayload(LogicConfigurationLoadFailureReason.CorruptConfig);

            var roundtripped = JsonSerializer.Deserialize<LogicConfigurationLoadFailedPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual(LogicConfigurationLoadFailureReason.CorruptConfig, roundtripped.Reason);
        }
    }
}
