using System.Linq;
using System.Reflection;
using System.Text.Json;
using Vion.Contracts.Constants;
using Vion.Contracts.Events;
using Vion.Contracts.Events.CloudToMesh;

namespace Vion.Contracts.Test.Events.CloudToMesh
{
    [TestClass]
    public class StopServiceShould
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
            var schema = typeof(StopService).GetCustomAttribute<SchemaAttribute>();

            Assert.IsNotNull(schema, "command must carry a [Schema] attribute");
            Assert.AreEqual("StopService", schema.Schema);
        }

        [TestMethod]
        public void RoundtripTheInstanceIdentifyingArgument()
        {
            var command = new StopService([
                new ServiceArgument(RemoteAccessConstants.Arguments.SessionId, "0195f0d1-1111-7abc-8def-000000000001"),
            ]);

            var roundtripped = JsonSerializer.Deserialize<StopService>(JsonSerializer.Serialize(command, WireOptions), WireOptions)!;

            var argument = roundtripped.Arguments.Single();
            Assert.AreEqual(RemoteAccessConstants.Arguments.SessionId, argument.Name);
            Assert.AreEqual("0195f0d1-1111-7abc-8def-000000000001", argument.Value);
        }
    }
}
