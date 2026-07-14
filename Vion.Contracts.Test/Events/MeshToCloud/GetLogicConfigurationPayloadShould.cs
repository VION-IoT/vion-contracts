using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Events;
using Vion.Contracts.Events.MeshToCloud;

namespace Vion.Contracts.Test.Events.MeshToCloud
{
    [TestClass]
    public class GetLogicConfigurationPayloadShould
    {
        // Mirrors the platform wire convention: camelCase property names.
        private static readonly JsonSerializerOptions WireOptions = new()
                                                                    {
                                                                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                                                                    };

        [TestMethod]
        public void CarryTheWireSchemaNameThatDrivesDispatch()
        {
            var schema = typeof(GetLogicConfigurationPayload).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "get body must carry a [Schema] attribute");
            Assert.AreEqual("GetLogicConfigurationPayload", schema.Schema);
        }

        [TestMethod]
        public void RoundTripTheAppliedHash()
        {
            var payload = new GetLogicConfigurationPayload("sha256:abc123");

            var roundtripped = JsonSerializer.Deserialize<GetLogicConfigurationPayload>(JsonSerializer.Serialize(payload, WireOptions), WireOptions)!;

            Assert.AreEqual("sha256:abc123", roundtripped.AppliedHash);
        }

        [TestMethod]
        public void TreatAnAbsentAppliedHashAsNull()
        {
            var roundtripped = JsonSerializer.Deserialize<GetLogicConfigurationPayload>("{}", WireOptions)!;

            Assert.IsNull(roundtripped.AppliedHash);
        }
    }
}
